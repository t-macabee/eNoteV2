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
1. Copy `.env.example` to `.env` and set `ConnectionStrings__DefaultConnection`.
   - **Local SQL Server (Windows auth):** `Server=localhost;Database=IB150057;Trusted_Connection=True;...`
   - **Docker SQL Server:** start `docker compose up -d` in this folder, then use the Docker connection string from `.env.example`.
2. Restore packages: `dotnet restore`
3. Update database: `dotnet ef database update -s eNote.API -p eNote.Infrastructure`
4. Run the API: `dotnet run --project eNote.API`

Seed users (Development only): `instructor` / `student` / `storeemployee` — password `Test1234!`

## Notes
- Secrets should be provided via environment variables or a secret store; do not hardcode them in appsettings.json.
- To enable worker/messaging, add a separate worker project and configure RabbitMQ in docker-compose.
- After schema changes, generate EF migrations from the Infrastructure project.
