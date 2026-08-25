using eNote.API.Consumers;
using eNote.API.Converters;
using eNote.API.Extensions;
using eNote.API.Hubs;
using eNote.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

DependencyInjection.LoadEnvironment();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseApplicationLogging();
builder.Configuration.ValidateRequiredSettings();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider((context, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

builder.Services
    .AddInfrastructure(builder.Configuration, bus =>
    {
        bus.AddConsumer<RentalStatusChangedPushConsumer>();
        bus.AddConsumer<LectureCancelledPushConsumer>();
        bus.AddConsumer<SubmissionGradedPushConsumer>();
    }, registerNotificationOutboxPublisher: false)
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices(builder.Configuration)
    .AddApplicationCors(builder.Configuration, builder.Environment)
    .AddApplicationRateLimiting()
    .AddResponseCompression(opts => opts.EnableForHttps = true)
    .AddMapsterMappings()
    .AddApplicationValidation()
    .AddApplicationApiVersioning();

builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        x.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });

builder.Services.AddInfrastructureHealthChecks();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseResponseCompression();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "instruments")),
    RequestPath = "/instruments"
});
app.UseCors(CorsExtensions.PolicyName);
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    await app.InitializeDevelopmentDataAsync();

    app.MapOpenApi().WithDocumentPerVersion();

    app.MapScalarApiReference(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.AddDocument(description.GroupName, description.GroupName);
        }
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<eNote.API.Middleware.TenantInitializationMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.HubPath);

app.Run();
