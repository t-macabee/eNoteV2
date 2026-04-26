# eNote Architecture Conventions Checklist

Use this checklist when adding or refactoring features in `eNote.API`, `eNote.Application`, `eNote.Domain`, and `eNote.Infrastructure`.

## 1) Naming and Language

- [ ] Use one term for the same concept across all layers.
  - Standardize on `MusicStore` (do not mix with `MusicShop`).
- [ ] Keep type names aligned with folder names.
  - Example: `Features/MusicStores/Instruments/...` -> `InstrumentService`, `InstrumentDto`, `InstrumentSearchObject`.
- [ ] Keep request and response naming explicit.
  - `CreateRequest`, `UpdateRequest`, `Dto`, `SearchObject`.
- [ ] Keep exception/user-facing message language consistent (single language policy).

## 2) Layer Boundaries

- [ ] `Domain` contains entities, enums, and domain rules/invariants.
- [ ] `Application` contains use-case orchestration, validation, and DTO mapping.
- [ ] `Infrastructure` contains EF Core, Identity, persistence configuration, and external integrations.
- [ ] `API` contains routing, HTTP contracts, and auth attributes only (no business logic).
- [ ] `API` should not need knowledge of many concrete infra types. Prefer extension registration from infrastructure.

## 3) Feature Folder Shape (Application)

For each feature, prefer this structure:

`Features/<BoundedContext>/<Feature>/`

- [ ] `DTOs/`
- [ ] `Requests/`
- [ ] `Search/`
- [ ] `Services/`
- [ ] `Interfaces/` (only where useful)

Rules:

- [ ] Keep all feature-specific types under the same feature folder.
- [ ] Do not place feature-specific code under `Common`.
- [ ] `Common` is only for truly cross-feature primitives (paging, clock, base abstractions).

## 4) Interface Usage

- [ ] Create interfaces at architectural boundaries (API -> Application, Application -> Infrastructure).
- [ ] Avoid interfaces that only mirror one concrete class without substitution value.
- [ ] Avoid pass-through wrappers that only forward method calls.
  - If a wrapper exists, it must add orchestration, policy, caching, events, or composition logic.
- [ ] Keep placement consistent:
  - Either always `Services/Interfaces` or always sibling folder style for interfaces in a feature.

## 5) Service Patterns

- [ ] Use generic base services/controllers for simple CRUD-heavy features.
- [ ] Use explicit workflow services for stateful/behavior-heavy features (example: rentals).
- [ ] If using Command/Query split, keep it where behavior complexity is high.
- [ ] Keep business rule checks centralized (prefer domain methods for core invariants when practical).

## 6) Data Access and Querying

- [ ] Reuse query include helpers (`WithRentalDetails`, `WithInstrumentDetails`) when the same graph is needed.
- [ ] Prefer projection for read models where possible.
- [ ] Keep search filters in one place (`AddFilter` or dedicated query service methods).
- [ ] Enforce key business uniqueness in database constraints, not only in app checks.
  - Example: filtered unique indexes for rental conflict scenarios.

## 7) Exception and API Error Conventions

- [ ] Define and use a small exception taxonomy:
  - `NotFound` -> 404
  - `Validation`/`BadRequest` -> 400
  - `Conflict` -> 409
  - `Forbidden` -> 403
  - `Unauthorized` -> 401
- [ ] Do not use `InvalidOperationException` for authorization failures.
- [ ] Keep middleware mapping aligned with service-level exceptions.
- [ ] Ensure concurrency and unique-index violations return stable business-friendly messages.

## 8) Mapping Conventions

- [ ] Keep all mappings in one predictable place (`Application/Mapping`).
- [ ] Do not duplicate mapping logic across services unless needed for performance reasons.
- [ ] Keep DTO property names aligned with the chosen domain vocabulary (`MusicStore`, not `MusicShop`).

## 9) Dependency Injection and Composition

- [ ] Group registrations by layer (`AddApplication`, `AddInfrastructure`, `AddApi` style).
- [ ] Keep `Program.cs` mostly declarative and short.
- [ ] Register only required abstractions; remove dead registrations and wrappers.

## 10) Consistency and Maintainability

- [ ] One feature, one obvious place to find controllers/services/DTOs.
- [ ] One naming convention for folder and file casing.
- [ ] One policy for async methods:
  - no `async` without `await` unless intentionally returning a task from a delegate contract.
- [ ] Add tests for workflow transitions and conflict paths before expanding workflow complexity.

---

## Immediate Cleanup Backlog (Tailored to Current Repo)

- [ ] Decide and apply `MusicStore` vs `MusicShop` naming globally (recommend: `MusicStore`).
- [ ] Remove `IRentalService` + `RentalService` pass-through wrapper, or give it real orchestration responsibilities.
- [ ] Standardize interface placement pattern in feature folders.
- [ ] Introduce explicit conflict/forbidden exception types and update middleware mapping.
- [ ] Add DB-level protection for duplicate pending rental requests per student/instrument.
