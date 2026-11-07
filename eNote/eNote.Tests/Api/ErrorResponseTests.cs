using eNote.API.Extensions;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace eNote.Tests.Api;

public sealed class ErrorResponseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ErrorHandling_ReturnsConflictBody_ForUsernameTaken()
    {
        await using var app = CreateApp(() => throw new ConflictException(Messages.UsernameTaken));
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/conflict");

            Assert.Equal(409, (int)response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(409, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("error.conflict", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(Messages.UsernameTaken, body.RootElement.GetProperty("message").GetString());
            Assert.False(body.RootElement.TryGetProperty("errors", out _));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task ErrorHandling_ReturnsNotFoundBody_ForMissingUser()
    {
        await using var app = CreateApp(() => throw new NotFoundException(Messages.NotFound));
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/missing");

            Assert.Equal(404, (int)response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("error.not_found", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(Messages.NotFound, body.RootElement.GetProperty("message").GetString());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task ErrorHandling_ReturnsInternalErrorBody_ForArgumentException()
    {
        await using var app = CreateApp(() => throw new ArgumentException("Invalid argument."));
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/conflict");

            Assert.Equal(500, (int)response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("error.internal", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(Messages.InternalError, body.RootElement.GetProperty("message").GetString());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task ErrorHandling_ReturnsTraceId_ForAnUnexpectedFailure()
    {
        await using var app = CreateApp(() => throw new InvalidOperationException("Disk failure."));
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/conflict");

            Assert.Equal(500, (int)response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task ErrorHandling_ReturnsInternalErrorBody_ForArgumentOutOfRangeException()
    {
        await using var app = CreateApp(() => throw new ArgumentOutOfRangeException("value"));
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/conflict");

            Assert.Equal(500, (int)response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("error.internal", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(Messages.InternalError, body.RootElement.GetProperty("message").GetString());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public void InvalidModelStateFactory_ReturnsStatusStatusCodeMessageAndErrors()
    {
        using var provider = CreateValidationProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email je obavezan.");
        modelState.AddModelError("Email", "Email nije validan.");
        modelState.AddModelError("Password", "Lozinka je obavezna.");

        var context = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            modelState);

        var result = Assert.IsType<BadRequestObjectResult>(options.InvalidModelStateResponseFactory(context));
        var body = Assert.IsType<eNote.API.ErrorResponse>(result.Value);

        Assert.Equal(400, body.Status);
        Assert.Equal("error.validation", body.Code);
        Assert.Equal("Email je obavezan.\nEmail nije validan.\nLozinka je obavezna.", body.Message);
        Assert.Equal(["Email", "Password"], body.Errors!.Keys);
        Assert.Equal(2, body.Errors["Email"].Length);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(body, JsonOptions));
        Assert.True(json.RootElement.TryGetProperty("status", out _));
        Assert.True(json.RootElement.TryGetProperty("code", out _));
        Assert.True(json.RootElement.TryGetProperty("message", out _));
        Assert.True(json.RootElement.TryGetProperty("errors", out _));
        Assert.Equal(2, json.RootElement.GetProperty("errors").GetProperty("Email").GetArrayLength());
    }

    [Fact]
    public void ApiBehaviorOptions_SuppressMapClientErrors()
    {
        using var provider = CreateValidationProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

        Assert.True(options.SuppressMapClientErrors);
    }

    [Fact]
    public void InvalidModelState_ReturnsFileTooLarge_WhenTheRequestExceedsItsLimit()
    {
        using var provider = CreateValidationProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "Failed to read the request form. Request body too large.");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "multipart/form-data; boundary=x";
        httpContext.Request.ContentLength = 6_291_457;
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(new FixedBodySizeFeature(6_291_456));
        var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);

        var result = Assert.IsType<ObjectResult>(options.InvalidModelStateResponseFactory(context));
        var body = Assert.IsType<eNote.API.ErrorResponse>(result.Value);

        Assert.Equal(413, result.StatusCode);
        Assert.Equal(413, body.Status);
        Assert.Equal(Messages.FileTooLarge, body.Message);
    }

    [Fact]
    public void InvalidModelState_HidesFormReadErrorText_WhenTheFormIsBroken()
    {
        using var provider = CreateValidationProvider();
        var options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "Failed to read the request form. Unexpected end of Stream.");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "multipart/form-data; boundary=x";
        httpContext.Request.ContentLength = 100;
        var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);

        var result = Assert.IsType<BadRequestObjectResult>(options.InvalidModelStateResponseFactory(context));
        var body = Assert.IsType<eNote.API.ErrorResponse>(result.Value);

        Assert.Equal(400, body.Status);
        Assert.Equal(Messages.FileNotReadable, body.Message);
        Assert.DoesNotContain("Failed", body.Message);
    }

    [Fact]
    public async Task ErrorHandling_DoesNotLogAnError_ForABusinessRefusal()
    {
        var logs = new RecordingLoggerProvider();
        await using var app = CreateApp(() => throw new BusinessException(Messages.InvalidFileFormat), logs);
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/conflict");

            Assert.Equal(400, (int)response.StatusCode);
            Assert.DoesNotContain(LogLevel.Error, logs.Levels);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task ErrorHandling_LogsAnError_ForAnUnexpectedFailure()
    {
        var logs = new RecordingLoggerProvider();
        await using var app = CreateApp(() => throw new InvalidOperationException("Disk failure."), logs);
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/conflict");

            Assert.Equal(500, (int)response.StatusCode);
            Assert.Contains(LogLevel.Error, logs.Levels);
            Assert.DoesNotContain("Disk failure", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider, ILogger
    {
        public ConcurrentBag<LogLevel> Levels { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Levels.Add(logLevel);

        public void Dispose()
        {
        }
    }

    private sealed class FixedBodySizeFeature(long limit) : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => true;
        public long? MaxRequestBodySize { get => limit; set => throw new NotSupportedException(); }
    }

    private static ServiceProvider CreateValidationProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationValidation();
        services.AddControllers();

        return services.BuildServiceProvider();
    }

    private static WebApplication CreateApp(Func<Exception> exceptionFactory, ILoggerProvider? loggerProvider = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        if (loggerProvider is not null)
        {
            builder.Logging.AddProvider(loggerProvider);
        }

        var app = builder.Build();
        app.UseErrorHandling();
        app.MapGet("/conflict", (HttpContext _) => throw exceptionFactory());
        app.MapGet("/missing", (HttpContext _) => throw exceptionFactory());
        return app;
    }
}
