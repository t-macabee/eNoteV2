using eNote.Application.Common.Interfaces;
using eNote.Infrastructure;
using eNote.Infrastructure.Messaging;
using eNote.Worker;
using eNote.Worker.Consumers;
using eNote.Worker.Extensions;
using eNote.Worker.Health;
using Serilog;

DependencyInjection.LoadEnvironment();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/enote-worker-bootstrap-.json", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

builder.Services.AddApplicationLogging(builder.Configuration);

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Fail fast instead of silently connecting to RabbitMqConfiguration's
// "localhost"/"guest" fallbacks when the env vars are missing entirely — the
// API already does the equivalent check in ConfigurationExtensions.ValidateRequiredSettings.
var rabbitMqError = RabbitMqConfiguration.GetMissingConfigurationError(builder.Configuration);

if (rabbitMqError is not null)
{
    throw new InvalidOperationException("Missing required configuration: " + rabbitMqError);
}

builder.Services.AddScoped<ICurrentActor, WorkerActor>();
builder.Services.AddInfrastructure(builder.Configuration, bus => bus.AddConsumer<RentalStatusChangedConsumer>());
builder.Services.AddInfrastructureHealthChecks();
builder.Services.AddHostedService<DatabaseHeartbeatService>();

IHost host = builder.Build();
host.Run();
