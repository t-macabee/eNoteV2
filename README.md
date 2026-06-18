# eNote

Backend for the eNote graduation project (FIT RS2, student index **IB150057**).

## Repository layout

```
eNoteV2/                 Git repository root
├── .github/workflows/   CI (build + test on push/PR)
└── eNote/               .NET solution (API, Worker, Domain, …)
```

Open the **`eNoteV2`** folder in your IDE (not only the inner `eNote` folder) so paths and launch configs stay consistent.

## Prerequisites

- .NET 10 SDK
- SQL Server (local instance or LocalDB)
- RabbitMQ (for rental notifications via the Worker)

## Environment variables

Copy `eNote/.env.example` to `eNote/.env` and adjust for your machine.

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__Key` | JWT signing secret (min 32 characters) |
| `RabbitMQ__Host`, `RabbitMQ__User`, `RabbitMQ__Password` | MassTransit / Worker messaging |
| `Cors__AllowedOrigins__0` | Required in production; optional in Development |

Do not commit `eNote/.env` or put real secrets in tracked `appsettings.json`.

## Run locally

From the repository root:

```bash
cd eNote
dotnet restore
dotnet ef database update -s eNote.API -p eNote.Infrastructure
dotnet run --project eNote.API
```

In a second terminal (same `eNote` directory):

```bash
dotnet run --project eNote.Worker
```

In **Development**, the API runs migrations on startup and seeds demo data.

| User | Password | Role |
|------|----------|------|
| `admin` | `Test1234!` | Admin |
| `instructor` | `Test1234!` | Instructor |
| `student` | `Test1234!` | Student |
| `storeemployee` | `Test1234!` | Store employee |

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

List endpoints support `page` and `pageSize` (default `page=1`, `pageSize=20`, max `100`).

## Messaging

Rental status changes are published to RabbitMQ after the API transaction commits. The **Worker** consumes `RentalStatusChanged` events and persists rows in the `Notification` table.

## Notes

- After schema changes, add EF migrations from the Infrastructure project (`-p eNote.Infrastructure`, startup project `-s eNote.API`).
- Recommender, Notifications API, Flutter client, and Docker Compose are planned in later phases.
