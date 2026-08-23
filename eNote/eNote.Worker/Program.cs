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

// Dev-only DI hardening: Host.CreateApplicationBuilder (generic host) already wires
// UseDefaultServiceProvider so that ValidateScopes + ValidateOnBuild are ON when the
// environment IsDevelopment and OFF otherwise. HostApplicationBuilder does not expose
// .Host, so this default cannot be re-stated explicitly here — it is relied upon.
// Safe because this worker registers no AddApplication()/IMapper-dependent services
// (see architecture-remediation-tasks.md §0.4 / Task E).

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var rabbitMqError = RabbitMqConfiguration.GetMissingConfigurationError(builder.Configuration);

if (rabbitMqError is not null)
{
    throw new InvalidOperationException("Missing required configuration: " + rabbitMqError);
}

builder.Services.AddScoped<ICurrentUserContext, WorkerActor>();
builder.Services.AddInfrastructure(builder.Configuration, bus =>
{
    bus.AddConsumer<RentalStatusChangedConsumer>();
    bus.AddConsumer<LectureCancelledConsumer>();
    bus.AddConsumer<SubmissionGradedConsumer>();
});
builder.Services.AddInfrastructureHealthChecks();
builder.Services.AddHostedService<DatabaseHeartbeatService>();

IHost host = builder.Build();
host.Run();
