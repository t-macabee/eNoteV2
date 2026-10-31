using eNote.Application.Common.Interfaces;
using eNote.Infrastructure;
using eNote.Worker;
using eNote.Worker.Extensions;
using eNote.Worker.Health;
using Serilog;

DependencyInjection.LoadEnvironment();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/enote-worker-bootstrap-.json", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

builder.Services.AddApplicationLogging(builder.Configuration);

builder.Services.AddScoped<ICurrentUserContext, WorkerActor>();
builder.Services
    .AddPersistence(builder.Configuration)
    .AddInfrastructureIdentity()
    .AddEmail(builder.Configuration)
    .AddMessaging(builder.Configuration, WorkerConsumers.Add);
builder.Services.AddRentalOptions(builder.Configuration);
builder.Services.AddRentalPickupExpiry();
builder.Services.AddInfrastructureHealthChecks();
builder.Services.AddHostedService<DatabaseHeartbeatService>();

IHost host = builder.Build();
host.Run();
