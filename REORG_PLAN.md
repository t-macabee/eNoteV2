# Reorganization Plan — eNoteV2 Structural Consistency Pass

Date: 2026-08-18
Solution: `eNote/eNote.sln` | Convention: `Features/<Domain>/<Subfeature>/` + Tests mirrors `Features/<Domain>` flat per-domain grouping

## Moves — grouped by project

### eNote.Tests (mirrored-structure defects)

| # | Current path | New path | Reason |
|---|--------------|----------|--------|
| T1 | `eNote.Tests/Courses/CourseEnrollmentServiceTests.cs` | `eNote.Tests/Academic/CourseEnrollmentServiceTests.cs` | Top-level `Courses/` violates mirror: service is `Features/Academic/Courses/Services/CourseEnrollmentService.cs`; siblings `CourseServiceTests.cs`/`RankingServiceTests.cs` already live under `Academic/` — merge to single domain folder. |
| T2 | `eNote.Tests/Users/CurrentActorTests.cs` | `eNote.Tests/Identity/CurrentActorTests.cs` | `Users/` split violates flat-domain mirror: all services under `Features/Identity/Users/Services/` (CurrentActor) are peers of `UserProvisioningService` etc. which already live under `Identity/`; consolidate to single domain folder `Identity/` matching `Academic`/`Communication` flat pattern. |
| T3 | `eNote.Tests/Users/UserProfileServiceTests.cs` | `eNote.Tests/Identity/UserProfileServiceTests.cs` | Same as T2 — `UserProfileService` lives beside `UserProvisioningService`/`UserIdentityService` under `Features/Identity/Users/Services/` with no sub-split in source. |
| T4 | `eNote.Tests/Users/UserSelfServiceTests.cs` | `eNote.Tests/Identity/UserSelfServiceTests.cs` | Same as T2 — `UserSelfService` peer of above. |
| — | `eNote.Tests/Users/` directory | *(removed after T2–T4)* | Empty after moves; delete. |
| — | `eNote.Tests/Courses/` directory | *(removed after T1)* | Empty after moves; delete. |

**Batching:** Batch A = T1 alone (one namespace `eNote.Tests.Courses→Academic`). Batch B = T2–T4 together (three namespaces `eNote.Tests.Users→Identity`). Each batch updates namespace declaration; no `using eNote.Tests.Courses/Users` references found elsewhere, so only file-internal namespace changes required. Build/test after each batch.

**Namespace fix detail:** Change `namespace eNote.Tests.Courses;` → `namespace eNote.Tests.Academic;` and `namespace eNote.Tests.Users;` → `namespace eNote.Tests.Identity;` in moved files. No csproj globs to update (SDK-style default glob includes `**/*.cs`).

### eNote.Application / eNote.Infrastructure / eNote.API — no moves in this pass

| Area | Verdict |
|------|---------|
| `eNote.Application/Features/Mapping/*MappingConfig.cs` (5 files) | **Left in place** — see reasoning below. |
| `eNote.Infrastructure/Data/Configurations/*.cs` (flat 22 configs) | **Left in place** — flat is conventional for EF `IEntityTypeConfiguration<>`; feature-grouping would obscure discoverability and is not established elsewhere in Infrastructure. |
| `eNote.Application/Validation/**` | **Left in place** — already per-domain (`Academic/`, `Identity/`, `Rentals/`, `Communication/`) mirroring Features domains; full `Features/<Domain>/<Subfeature>` depth not applied in Tests either, so consistent. |
| `eNote.API/Controllers/**`, `Extensions/**` | **Left in place** — no business logic found; controllers delegate to Application services; extensions only wire DI/Mapster/CORS/Validation. |
| `eNote.Tests/InstrumentRentals/RecommendationServiceTests.cs` | **Left in place** — service `Features/Rentals/Recommendations/Services/RecommendationService.cs` is sibling of `InstrumentRentals` under `Rentals`; current `InstrumentRentals/` top-level is the established mirror for `Rentals/InstrumentRentals` (omitting `Rentals/` prefix like `Academic` omits subfeature depth). Moving Recommendation alone would invent `Tests/Rentals/` hierarchy not used elsewhere — flag as minor drift, not a must-fix. |
| `eNote.Tests` missing folders for `Rentals/Instruments` and `Rentals/ReferenceData` | **No move — genuinely missing tests**, not misplaced (see §4). Out of scope to create. |

## Deliberately left in place — inspected files with reasoning

1. **Mapping bucket (`Features/Mapping/`):** Each config imports an unrelated domain (`Announcement`, `Course`, `Instrument`, `InstrumentRental`, `Lecture`) and implements `Mapster.IRegister`. `eNote.API/Extensions/MapsterExtensions.cs:13` does `config.Scan(typeof(CourseMappingConfig).Assembly)` — assembly scan discovers all `IRegister` regardless of namespace/folder, so shared bucket is *not* required for registration. However moving each profile beside its domain would create 5 one-file subfolders fragmented across `Academic/*`, `Rentals/*`, `Communication/*`, increase churn for zero functional gain, and Mapping is already a cross-cutting concern analogous to `Validation/` which is also centralized. Chose to **leave flat and document** rather than co-locate — both are defensible; consistency with existing Validation centralization tips toward leaving.

2. **Infrastructure `Data/Configurations/`:** Mirrors Validation centralization pattern; no feature grouping established in Infrastructure. Moving would be speculative.

3. **API `Extensions/ApplicationServiceExtensions.cs`, `MapsterExtensions.cs`, `Services/CurrentUserService.cs`:** `CurrentUserService` implements `Application.Common.Interfaces.ICurrentUserService` — correct layer: API composition root providing HttpContext-backed implementation. `ApplicationServiceExtensions` uses `Scrutor` scan `FromAssembliesOf(typeof(CourseService))` — legitimate composition-root wiring, not business logic bleed. No layer violation.

4. **ReferenceData / Instruments missing tests:** `grep -r "InstrumentService|AddressService|InstrumentTypeService|MusicStoreService" eNote.Tests` returns only `TenantIsolationTests` touching rentals. Services exist at `Features/Rentals/Instruments/Services/InstrumentService.cs`, `Features/Rentals/ReferenceData/Addresses/AddressService.cs`, `InstrumentTypes/InstrumentTypeService.cs`, `MusicStores/MusicStoreService.cs` — no tests found elsewhere, not misplaced. Flag as coverage gap, not a move.

5. **Validation & Communication mirrors:** `Validation/Academic|Identity|Rentals|Communication` and `Tests/Communication|Validation|Files|Domain|Data|Messaging|Storage` already follow domain-level mirroring; no moves needed.

## Layer-boundary violations — found but NOT fixed (logic change required, out of scope)

- **None found that require file moves.** Quick audit: `eNote.Domain` references only `System`/`Microsoft` + `Domain.Shared`; no `Application/Infrastructure/API/Worker/Contracts` refs. `Application` refs only `Domain` + `Contracts`. `Infrastructure` refs `Application` + `Domain`. `API`/`Worker` ref `Application`/`Infrastructure`/`Contracts`. No backward edge detected via `grep` of `using eNote.` across projects. Any business logic that *might* have crept into `ApiVersioningExtensions`/`RateLimitingExtensions` is config-only. Flagged for review but no move to perform.

## Execution process

1. Git mv + namespace edit per batch.
2. After each batch: `dotnet build eNote/eNote.sln` and `dotnet test eNote/eNote.sln` must pass before next batch.
3. If Program.cs/.csproj globs required update (none expected), do in same batch.
4. Final report confirms moved/left/violations and final build+test pass.

## Risks

- `git mv` preserves history; SDK glob means csproj untouched. Only risk is stale namespace if external script references `eNote.Tests.Courses/Users` — none found in repo.
