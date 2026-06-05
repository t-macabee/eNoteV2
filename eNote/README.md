# eNote API

This repository contains the eNote API project used for the seminar.

## Prerequisites
- .NET 10 SDK
- SQL Server instance
- (Optional) RabbitMQ and worker for microservice integration

## Required environment variables
- ConnectionStrings__DefaultConnection - SQL Server connection string
- JWT__Key - secret key used for JWT signing
- RABBITMQ__HOST - RabbitMQ host (if using worker)
- SMTP__HOST, SMTP__PORT, SMTP__USER, SMTP__PASS - SMTP settings (if used)

## Run locally
1. Restore packages: `dotnet restore`
2. Update database and run migrations:
   - `dotnet ef migrations add YourMigrationName -s eNote.API -p eNote.Infrastructure`
   - `dotnet ef database update -s eNote.API -p eNote.Infrastructure`
3. Run the API: `dotnet run --project eNote.API`

## Notes
- Secrets should be provided via environment variables or a secret store; do not hardcode them in appsettings.json.
- To enable worker/messaging, add a separate worker project and configure RabbitMQ in docker-compose.
- After schema changes, generate EF migrations from the Infrastructure project.
