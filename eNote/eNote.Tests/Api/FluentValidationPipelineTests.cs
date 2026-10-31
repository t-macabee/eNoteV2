using eNote.API.Extensions;
using eNote.Application.Features.Identity.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace eNote.Tests.Api;

public sealed class FluentValidationPipelineTests
{
    [Fact]
    public async Task InvalidLoginBody_ReturnsValidationBody()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var content = new StringContent("""{"username":"","password":"tajna"}""", Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/validation/login", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("error.validation", body.RootElement.GetProperty("code").GetString());
            Assert.Equal("Korisničko ime je obavezno.", body.RootElement.GetProperty("message").GetString());

            var errors = body.RootElement.GetProperty("errors");
            Assert.Equal(["Username"], errors.EnumerateObject().Select(property => property.Name));
            Assert.Equal(["Korisničko ime je obavezno."], errors.GetProperty("Username").EnumerateArray().Select(error => error.GetString()));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task ValidLoginBody_ReachesAction()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var content = new StringContent("""{"username":"student","password":"tajna"}""", Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/validation/login", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("student", body.RootElement.GetProperty("username").GetString());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task InvalidLoginBody_BindingFailure_ReturnsValidationBody()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var content = new StringContent("""{"username":123,"password":"tajna"}""", Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/validation/login", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("error.validation", body.RootElement.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("message").GetString()));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddApplicationValidation();
        builder.Services.AddControllers().AddApplicationPart(typeof(ValidationTestController).Assembly);
        var app = builder.Build();
        app.MapControllers();
        return app;
    }
}

[ApiController]
[Route("validation")]
public sealed class ValidationTestController : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<LoginRequest> Login([FromBody] LoginRequest request) => Ok(request);
}
