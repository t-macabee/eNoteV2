# eNote

ASP.NET Core backend for a music-school platform: courses, lectures, assignments, instrument rentals, and in-app notifications.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)
- SQL Server (local instance or Docker — see repo-root `docker-compose.yml`)
- RabbitMQ (local install or Docker)

## Quick start

1. Copy environment variables:

   ```bash
   cp .env.docker.example .env
   ```

   Adjust `ConnectionStrings__DefaultConnection`, `Jwt__Key` (min 32 characters), `Smtp__*`, and `STRIPE_*` values for your machine.

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

## Payments (Stripe)

Instrument rentals are billed once via Stripe when a rental reaches `Complete`
or `ReturnedEarly` (server-computed total, EUR by default). Requires
`STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` — see `.env.docker.example`
for the full list (`STRIPE_PUBLISHABLE_KEY`, `STRIPE_CURRENCY`,
`STRIPE_STATEMENT_DESCRIPTOR` are optional). Point your Stripe webhook (or
`stripe listen`) at `POST /api/v{version}/payments/stripe/webhook`.

| Endpoint | Role |
|----------|------|
| `POST /api/v{version}/student/rentals/{rentalId}/payments/create-intent` | Student |
| `GET /api/v{version}/student/rentals/{rentalId}/payments` | Student |
| `POST /api/v{version}/shop/rentals/{rentalId}/payments/refund` | StoreEmployee |
| `POST /api/v{version}/payments/stripe/webhook` | Stripe (signature-verified, anonymous) |

A refund does not reset a rental's paid status — once paid, a rental stays
paid; refunds are a store courtesy, not a reversal of the debt guard.
Students with an unpaid completed/returned rental are blocked from
requesting a new rental until it's paid.

## PDF reports

| Endpoint | Role |
|----------|------|
| `GET /api/admin/music-stores/report` | Administrator |
| `GET /api/instructor/courses/{courseId}/ranking/report` | Instructor |
| `GET /api/instructor/lectures/{id}/attendance/report` | Instructor |
| `GET /api/shop/rentals/report` | StoreEmployee |

## Reference data (checklist #4)

The following reference tables have dedicated CRUD screens under the Administrator role:

- **Cities** (`admin/cities`) — standalone FK table; `Address.CityId` references it via dropdown, never free text (§3.1).
- **Addresses** (`admin/addresses`) — street/number + City FK.
- **MusicStores** (`admin/music-stores`)
- **InstrumentTypes** (`admin/instrument-types`)

`Instructor` is intentionally read-only in `InstructorListScreen` — instructors are provisioned and fully managed through the single `AdminUsersController` / `UserProvisionFormScreen` flow (`role: "Instructor"`), which already provides add/edit and role-membership control. A second, duplicate CRUD for the same accounts would diverge rather than help.

`Country` / `Category` / `Status` tables do not exist in this domain by design: eNoteV2 has no physical product categories and no international addresses (all addresses are `City` → `Street` → `Number` within BiH). Adding empty Country/Category tables would be artificial normalization with no query, filter, or reporting use. This is the documented exception per checklist #4 — the domain's reference data is fully covered by the four tables above.

## Ranking search (Upute §2.2)

Ranking (`RankingScreen`) includes a debounced student-name text filter above the table, satisfying the per-list search parameter rule. The filter is client-side over the already-fetched ranking payload, so no extra API round-trip is needed.

## Validation messages (checklist #6)

All format-constrained fields now carry specific error text (not just “required”): `City.Name`, `Address.CityId/Street/Number`, `MusicStore.StoreName/BusinessHours`, `InstrumentType.Type/MonthlyFee`, `Course.Name/Price`, etc. — see `*RequestValidator` classes for the exact `WithMessage` texts.

## Minor / UX notes (P7)

- **Back navigation**: list screens are drawer-hosted (`MasterScreen` → `RoleMenu`), not pushed routes, so the drawer itself is the primary navigation. Form screens provide an explicit “X” close in the AppBar/dialog title per RS2 UX rule. No additional labeled “Back” is added to list screens — adding a pop-style back where there is no route to pop would be misleading.
- **Many-to-many IDs**: the only current many-to-many is `Course↔Student` via `Enrollment`; the ranking and attendance screens always render student display names, never bare IDs.
- **Images**: thumbnails remain at 40 px (≈5% of form width); no regression toward 50%+.

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