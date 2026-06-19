using eNote.Application.Common.Time;
using eNote.Infrastructure.Configuration;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Messaging;
using eNote.Worker.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

DotEnvConfiguration.Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddRabbitMqMassTransit(builder.Configuration, bus => bus.AddConsumer<RentalStatusChangedConsumer>());

IHost host = builder.Build();
host.Run();
