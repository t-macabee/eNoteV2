# eNote

ASP.NET Core backend for a music-school platform: courses, lectures, assignments, instrument rentals, and in-app notifications.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)
- PostgreSQL (local instance or Docker — see repo-root `docker-compose.yml`)
- RabbitMQ (local install or Docker)

## Quick start

1. Copy environment variables:

   ```bash
   cp .env.docker.example .env
   ```

   Adjust `ConnectionStrings__DefaultConnection`, `Jwt__Key` (min 32 characters), and `Smtp__*` values for your machine.

2. Restore and apply migrations (from this `eNote/` directory):

   ```bash
   dotnet restore
   dotnet ef database update --project eNote.Infrastructure --startup-project eNote.API
   ```

3. Run the API (applies migrations and seeds dev data automatically in Development):

   ```bash
   dotnet run --project eNote.API
   ```

4. Run the Worker (processes RabbitMQ messages and retries failed rental notifications):

   ```bash
   dotnet run --project eNote.Worker
   ```

API listens on `http://localhost:5059` (or ports in `launchSettings.json`). OpenAPI docs are available via Scalar in Development.

## Flutter frontend

The mobile/desktop client is in a separate repository. To point it at a running API:

```bash
flutter run --dart-define=API_BASE_URL=http://localhost:5059
```

Replace `http://localhost:5059` with the actual API address (or the `API_PORT` you set in `.env`).

## Docker

From the repository root:

```bash
docker compose up --build
```

See `.env.docker.example` at the repo root for required variables.

## Development seed users

On first run in Development, these accounts are created (password from `Seed__DefaultPassword`, default `Test1234!`):

| Username        | Role           | Email                    |
|-----------------|----------------|--------------------------|
| admin           | Administrator  | admin@enote.com          |
| instructor      | Instructor     | instructor@enote.com     |
| student         | Student        | student@enote.com        |
| storeemployee   | StoreEmployee  | storeEmployee@enote.com  |

## PDF reports

| Endpoint | Role |
|----------|------|
| `GET /api/instructor/courses/{courseId}/ranking/report` | Instructor |
| `GET /api/instructor/lectures/{id}/attendance/report` | Instructor |
| `GET /api/shop/rentals/report` | StoreEmployee |

## Tests

```bash
dotnet test
```

## Solution layout

| Project | Purpose |
|---------|---------|
| `eNote.Domain` | Entities and enums |
| `eNote.Application` | Business logic and services |
| `eNote.Infrastructure` | EF Core, Identity, messaging |
| `eNote.API` | HTTP API and SignalR |
| `eNote.Worker` | Background consumers |
| `eNote.Contracts` | Message contracts |
| `eNote.Tests` | Unit tests |