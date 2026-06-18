using eNote.Application.Common.Time;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Messaging;
using eNote.Worker.Extensions;
using Microsoft.EntityFrameworkCore;

eNote.Worker.Extensions.ConfigurationExtensions.LoadDotEnv();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");
}

if (!RabbitMqConfiguration.IsConfigured(builder.Configuration))
{
    throw new InvalidOperationException("RabbitMQ__Host is required.");
}

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddDbContext<ENoteContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure")));

builder.Services.AddWorkerMassTransit(builder.Configuration);

var host = builder.Build();
host.Run();
