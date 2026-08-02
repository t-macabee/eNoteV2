using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Time;
using eNote.Infrastructure.Configuration;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Messaging;
using eNote.Worker;
using eNote.Worker.Consumers;
using eNote.Worker.Extensions;
using eNote.Worker.Health;
using Microsoft.EntityFrameworkCore;
using Serilog;

DotEnvConfiguration.Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/enote-worker-bootstrap-.json", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

builder.Services.AddApplicationLogging(builder.Configuration);

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");
}

if (!RabbitMqConfiguration.IsConfigured(builder.Configuration))
{
    throw new InvalidOperationException("RabbitMQ__Host is required.");
}

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICurrentActor, WorkerActor>();
builder.Services.AddDbContext<ENoteContext>(options =>
options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure")));
builder.Services.AddScoped<eNote.Application.Common.Persistence.IAppDbContext>(sp => sp.GetRequiredService<ENoteContext>());

builder.Services.AddRabbitMqMassTransit(builder.Configuration, bus => bus.AddConsumer<RentalStatusChangedConsumer>());
builder.Services.AddHealthChecks().AddCheck<WorkerHealthCheck>("database");
builder.Services.AddHostedService<DatabaseHeartbeatService>();

IHost host = builder.Build();
host.Run();
