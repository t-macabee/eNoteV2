using eNote.Application.Common.Interfaces;
using eNote.Contracts.Assignments;
using eNote.Contracts.Communication;
using eNote.Contracts.Enrollments;
using eNote.Contracts.Identity;
using eNote.Contracts.Lectures;
using eNote.Contracts.Rentals;
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

var rabbitMqError = RabbitMqConfiguration.GetMissingConfigurationError(builder.Configuration);

if (rabbitMqError is not null)
{
    throw new InvalidOperationException("Missing required configuration: " + rabbitMqError);
}

builder.Services.AddScoped<ICurrentUserContext, WorkerActor>();
builder.Services.AddInfrastructure(builder.Configuration, bus =>
{
    bus.AddConsumer<RentalStatusChangedConsumer>();
    bus.AddConsumer<RentalStatusChangedEmailConsumer>();
    bus.AddConsumer<RentalRefundedConsumer>();
    bus.AddConsumer<LectureCancelledConsumer>();
    bus.AddConsumer<SubmissionGradedConsumer>();
    bus.AddConsumer<AnnouncementPublishedConsumer>();
    bus.AddConsumer<EnrollmentStatusChangedConsumer>();
    bus.AddConsumer<TuitionPaymentConsumer>();
    bus.AddConsumer<MembershipWarningConsumer>();
    bus.AddConsumer<MembershipWarningEmailConsumer>();
    bus.AddConsumer<FaultLoggingConsumer<RentalStatusChanged>>();
    bus.AddConsumer<FaultLoggingConsumer<RentalRefunded>>();
    bus.AddConsumer<FaultLoggingConsumer<LectureCancelled>>();
    bus.AddConsumer<FaultLoggingConsumer<SubmissionGraded>>();
    bus.AddConsumer<FaultLoggingConsumer<AnnouncementPublished>>();
    bus.AddConsumer<FaultLoggingConsumer<EnrollmentStatusChanged>>();
    bus.AddConsumer<FaultLoggingConsumer<TuitionPaymentNotification>>();
    bus.AddConsumer<FaultLoggingConsumer<MembershipWarning>>();
}, registerNotificationOutboxPublisher: false, validateJwtOptions: false);
builder.Services.AddInfrastructureHealthChecks();
builder.Services.AddHostedService<DatabaseHeartbeatService>();

IHost host = builder.Build();
host.Run();
