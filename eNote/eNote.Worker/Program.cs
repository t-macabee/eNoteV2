using eNote.Application.Common.Interfaces;
using eNote.Infrastructure;
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

builder.Services.AddScoped<ICurrentActor, WorkerActor>();
builder.Services.AddInfrastructure(builder.Configuration, bus => bus.AddConsumer<RentalStatusChangedConsumer>());
builder.Services.AddInfrastructureHealthChecks();
builder.Services.AddHostedService<DatabaseHeartbeatService>();

IHost host = builder.Build();
host.Run();
