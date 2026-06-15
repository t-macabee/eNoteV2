# eNote API

This repository contains the eNote API project used for the seminar (student index **IB150057**).

## Prerequisites
- .NET 10 SDK
- SQL Server instance (local or LocalDB)

## Required environment variables
- `ConnectionStrings__DefaultConnection` — SQL Server connection string
- `Jwt__Key` — secret key used for JWT signing (min 32 characters)
- `Cors__AllowedOrigins__0` — required in production; optional in Development

See `.env.example` for sample values.

## Run locally
1. Copy `.env.example` to `.env` and set `ConnectionStrings__DefaultConnection`.
   - **Local SQL Server (Windows auth):** `Server=localhost;Database=150057;Trusted_Connection=True;...`
   - **LocalDB:** uncomment the LocalDB line in `.env.example`.
2. Restore packages: `dotnet restore`
3. Apply migrations: `dotnet ef database update -s eNote.API -p eNote.Infrastructure`
4. Run the API: `dotnet run --project eNote.API`

In **Development**, the API also runs `Database.MigrateAsync()` on startup and seeds demo data.

Seed users (Development only): `instructor` / `student` / `storeemployee` — password `Test1234!`

API docs: `http://localhost:5059/scalar` (Development)

## Main API areas
| Area | Instructor routes | Student routes | Store routes |
|------|-------------------|----------------|--------------|
| Courses | `GET/POST/PUT/DELETE /api/instructor/courses` | `GET /api/student/courses`, `POST .../enroll`, `POST .../unenroll` | — |
| Lectures | `GET/POST/PUT/DELETE /api/instructor/lectures`, `POST .../cancel`, attendance | `GET /api/student/lectures`, `POST .../rsvp` | — |
| Announcements | `GET/POST/PUT/DELETE /api/instructor/courses/{courseId}/announcements` | `GET /api/student/announcements` | `GET/POST/PUT/DELETE /api/shop/announcements` |
| Assignments | `GET/POST/PUT/DELETE /api/instructor/lectures/{lectureId}/assignments` | `GET/POST /api/student/assignments` | — |
| Lecture notes | `GET/POST/PUT/DELETE /api/instructor/lectures/{lectureId}/notes` | `GET /api/student/lectures/{lectureId}/notes` | — |
| Instruments & rentals | — | `GET/POST /api/student/rentals` | `GET/POST/PUT/DELETE /api/shop/instruments`, rental actions on `/api/shop/rentals` |
| Public catalog | — | `GET /api/instruments/public` | — |

List endpoints support `page` and `pageSize` query parameters (default `page=1`, `pageSize=20`, max `100`). Instrument and rental list endpoints accept the same paging fields via their search query object.

## Notes
- Secrets should be provided via environment variables or a secret store; do not hardcode them in appsettings.json.
- Recommender and RabbitMQ worker are planned separately.
- After schema changes, generate EF migrations from the Infrastructure project.
