using eNote.API.Realtime.Consumers;
using eNote.API.Converters;
using eNote.API.Extensions;
using eNote.API.Hubs;
using eNote.Infrastructure;
using eNote.Infrastructure.Data.Seed;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

DependencyInjection.LoadEnvironment();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseApplicationLogging();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider((context, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

builder.Services
    .AddPersistence(builder.Configuration)
    .AddInfrastructureIdentity()
    .AddEmail(builder.Configuration)
    .AddMessaging(builder.Configuration, bus =>
    {
        bus.AddConsumer<RentalStatusChangedPushConsumer>();
        bus.AddConsumer<RentalRefundedPushConsumer>();
        bus.AddConsumer<LectureCancelledPushConsumer>();
        bus.AddConsumer<SubmissionGradedPushConsumer>();
        bus.AddConsumer<AnnouncementPublishedPushConsumer>();
        bus.AddConsumer<EnrollmentStatusChangedPushConsumer>();
        bus.AddConsumer<TuitionPaymentPushConsumer>();
        bus.AddConsumer<MembershipWarningPushConsumer>();
    })
    .AddApiInfrastructure(builder.Configuration)
    .AddJwtAuthentication()
    .AddAuthorization()
    .AddApplicationServices(builder.Configuration)
    .AddApplicationCors(builder.Configuration)
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

app.Services.GetRequiredService<IStartupValidator>().Validate();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseResponseCompression();
}

app.UseCors(CorsExtensions.PolicyName);
app.UseErrorHandling();
app.UseMiddleware<eNote.API.Middleware.AuthRateLimitIdentityMiddleware>();

if (app.Services.GetRequiredService<IOptions<SeedOptions>>().Value.Enabled)
{
    await app.InitializeDevelopmentDataAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();

    app.MapScalarApiReference(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.AddDocument(description.GroupName, description.GroupName);
        }
    });
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<eNote.API.Middleware.TenantInitializationMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.HubPath);

app.Run();
