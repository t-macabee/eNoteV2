# eNoteV2 — Phase 3: Refactor Procedure

**Generated**: 2026-06-28
**Basis**: [Phase2_Architecture_Audit.md](Phase2_Architecture_Audit.md) + full call-site analysis of the live source
**Lens**: best-for-the-code — decisions are judged on cohesion and correct concept boundaries, not on diff size or whether a metric gets inspected. A green NDepend gate is a *byproduct* of correct design here, never the goal.

> Replaces the earlier churn-hedged draft. The headline refactor below is recommended because it is the right design, proven against every call site — not because it satisfies a rule.

---

## 0. What's left in the overall plan

| Phase | State | Artifact |
|---|---|---|
| 1 — Mental Model | ✅ Done | [Phase1_Mental_Model.md](Phase1_Mental_Model.md) |
| 2 — Architecture Audit | ✅ Done | [Phase2_Architecture_Audit.md](Phase2_Architecture_Audit.md) |
| **3 — Refactor** | **Procedure written, execution pending go** | this doc |
| 4 — Style Audit | ⏳ Not started | method-level verbosity, naming, dead abstractions |

Three workstreams, independent, in priority order: **(1)** the identity/actor redesign, **(2)** EF config separation, **(3)** two correctness fixes. Do them as separate commits.

---

## 1. Workstream 1 — Dissolve `IUserContextResolver` into cohesive roles

### 1.1 The design defect (proven against call sites)

`IUserContextResolver` is a six-method grab-bag that **conflates three unrelated concepts**, and `IMusicStoreContextService` is a thin wrapper whose parameter is vestigial. The evidence:

| Concept actually being served | Call sites | Proof |
|---|---|---|
| **Current actor's own profile** (self-scoped) | ~20 | every call passes `currentUserService.UserId`: `resolver.GetCurrentStudentIdAsync(currentUserService.UserId)` across Rental, Course, Ranking, Lecture, Assignment, Recommendation, Enrollment, LectureNote, Instrument |
| **Any user's profile by id** (arbitrary) | `UserProfileService` (admin viewing any profile via public `GetUserAsync(int)`), `FileAccessService` | `userId` is a method parameter, not the current principal |
| **Display name** (presentation) | ReportService, LectureAttendance, AssignmentSubmission, Ranking | methods take `Student` entities, return formatted names |
| **Active store scope** (self-scoped) | 10 — *all* of `IMusicStoreContextService` | every call passes `currentUserService.UserId`; the `userId` param is never anything else |

A service should not assemble "the current actor" from three injected pieces, and one interface should not answer "who am I", "look up anyone", and "what's their display name" at once. Those are different questions with different lifetimes.

### 1.2 Target design — four cohesive types

```csharp
// 1. Ambient identity. The ~5 id-only consumers depend on THIS and nothing more.
public interface ICurrentUser
{
    int UserId { get; }
    bool IsAuthenticated { get; }
}

// 2. The acting principal, self-scoped, lazy, MEMOIZED per request.
public interface ICurrentActor : ICurrentUser
{
    Task<Student> GetStudentAsync();
    Task<int> GetCurrentStudentIdAsync();
    Task<Instructor> GetInstructorAsync();
    Task<MusicStoreEmployee> GetActiveEmployeeAsync();
    Task<int> GetActiveStoreAsync();
}

// 3. Look up ANY user's profile by id — genuinely distinct from "the current actor".
public interface IUserProfileLookup
{
    Task<Student> GetStudentAsync(int userId);
    Task<Instructor> GetInstructorAsync(int userId);
    Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId);
}

// 4. Presentation: format display names. Pure, no actor involved.
public interface IStudentDisplayNameService
{
    Task<string> GetStudentDisplayNameAsync(Student student);
    Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students);
}
```

**Implementation layering** (each type one job):
- `ICurrentUser` → reads HttpContext (API) / message headers (Worker). *(the existing `CurrentUserService`, renamed)*
- `IUserProfileLookup` → the by-id DB queries (the old resolver's `GetStudentAsync(id)` etc.)
- `ICurrentActor` → composes `ICurrentUser` + `IUserProfileLookup` + the store query, and **memoizes the resolved profile for the request** (scoped lifetime). This fixes a real latent inefficiency: services that call `GetCurrentStudentIdAsync` today re-run a full `Student` query on every call within one request.
- `IStudentDisplayNameService` → the old resolver's display methods (uses `IUserIdentityService`).

`IUserContextResolver` and `IMusicStoreContextService` are **deleted** once empty.

> **Net type count goes +1** (two grab-bag interfaces → four cohesive ones). Under a "fewest types" lens that loses; under best-for-the-code it wins, because each type now answers exactly one question. The arbitrary-id case is precisely why a single mega-facade would be wrong — `UserProfileService` serving admin profile views is not "the current actor."

### 1.3 Constructor outcomes (byproduct, not target)

| Service | Before | After | Remaining deps (all distinct concerns) |
|---|---|---|---|
| `RentalCommandService` | 8 | **6** | context, mapper, clock, currentActor, stateMachine, dispatcher |
| `AnnouncementService` | 8 | **6** | context, clock, currentActor, instructorAccess, fileStorage, mapper |
| `ReportService` | 7 | **6** | context, clock, ranking, instructorAccess, currentActor, displayNames |

NDepend ND1004 clears on its own. We never touch the threshold.

### 1.4 Migration order (by the call-site map)

Each batch compiles and tests green before the next.

1. **Introduce the four interfaces + impls; register in DI** (scoped). Leave the old two in place temporarily so nothing breaks yet.
2. **Self-scoped resolver consumers** → `ICurrentActor`, dropping their separate `currentUserService` + `resolver` injections:
   `RentalCommandService`, `AnnouncementService`, `CourseEnrollmentService`, `CourseService`, `RankingService`, `LectureService`, `LectureNoteService`, `LectureAttendanceService`, `AssignmentService`, `AssignmentSubmissionService`, `RecommendationService`, `InstrumentService`, `InstructorAccessService` (its `GetCurrentInstructorIdAsync`).
3. **Store consumers** → `ICurrentActor.GetActiveStoreAsync()`:
   `RentalQueryService`, `RentalCommandService`, `AnnouncementService`, `ReportService`. Then **delete `IMusicStoreContextService`**.
4. **Display-name consumers** → `IStudentDisplayNameService`:
   `ReportService`, `LectureAttendanceService`, `AssignmentSubmissionService`, `RankingService`.
5. **Arbitrary-id consumers** → `IUserProfileLookup`:
   `UserProfileService` (the admin profile path), `FileAccessService` (by-id authz check — keep its `userId` parameter; it is a pure authz function, not the actor).
6. **Id-only consumers** → `ICurrentUser` (renamed from `ICurrentUserService`):
   `NotificationService`, `UserSelfService`, `UserProfileService` (its `GetCurrentUserAsync`), `UploadsController`.
7. **Delete `IUserContextResolver`** once it has zero references. Confirm with a solution-wide search.

**Check after each batch:** `dotnet build eNote/eNote.sln` (0/0) + `dotnet test eNote/eNote.sln`.

---

## 2. Workstream 2 — Separate EF configuration (best-for-code, recommended)

Phase 2 §5 flagged `Infrastructure.Data` at debt rating D, driven by `ENoteContext.OnModelCreating` referencing every entity namespace.

**This is worth doing on the merits, not to clear the rating.** Move each entity's Fluent configuration into its own `IEntityTypeConfiguration<TEntity>` class, then replace the body of `OnModelCreating` with `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);`. Benefits independent of any metric: per-entity config is testable in isolation, diffs stay local when one entity's mapping changes, and the context stops being a god-method. The D rating clears as a byproduct.

**Check:** generate a migration diff (`dotnet ef migrations add _verify_noop`) — it must be **empty**, proving the config move changed no schema. Discard the throwaway migration.

---

## 3. Workstream 3 — Correctness fixes (independent, small)

### 3.1 Explicit culture on PDF formatting (ND2700)
[ReportService.cs:65,130,131](eNote/eNote.Application/Features/Reports/Services/ReportService.cs) — `AverageGrade?.ToString("F2")`, `Fee.ToString("F2")`, `TotalFee?.ToString("F2")` use ambient `CurrentCulture`. In a container the locale is unpinned; the decimal separator can shift silently — the same latent-Docker bug class the Protocol calls out for `DateTime`.

**Decision (locked): explicit `bs-BA`.** The reports are wholly Bosnian documents (`Rang lista`, `Naknada`, `Ukupno`) containing money and grades; the date footers already use an explicit `dd.MM.yyyy` format, so these `F2` decimals are the only ambient-culture leak. Invariant would render anglicized dots (`120.00`) inside an otherwise-Bosnian PDF. Define one `static readonly CultureInfo` (e.g. `CultureInfo.GetCultureInfo("bs-BA")`) for report formatting and pass it to every `ToString("F2", reportCulture)`. Never rely on ambient.

> Adjacent, deferred: the `Naknada`/`Ukupno` columns render a bare `120,00` with no currency unit. Adding `KM`/`BAM` is a product call, out of scope here.

### 3.2 Read-only exposed collections (ND2300, ND1908 — 5 sites)
Open the two rules in NDepend for exact `file:line`. Per site:
- Entity nav collections → `IReadOnlyCollection<T>` over a private backing field; mutate only via entity methods (matches the existing rich-entity idiom — `Approve`/`Pickup`/`MarkRead`).
- DTO/record collections → `IReadOnlyList<T>` init-only.
- The `InstrumentRentalStatusSets.Blocking` `readonly[]` → `FrozenSet<T>` (it is `.Contains`-heavy on the rental hot path — a correctness *and* fit improvement).

**Check:** any test mutating one of these directly will fail to compile — route it through the proper method; that is the fix landing.

---

## 4. On the EF namespace cycles (ND1400) — not a Phase 3 defect

Phase 2's other "critical" rule. On the merits: bidirectional navigation properties (`Student.InstrumentRentals` ↔ `InstrumentRental.Student`) are **correct domain modeling**. The "cycle" exists only because entities are split into per-feature sub-namespaces; it is a namespace-granularity artifact, entirely within Domain, zero runtime cost. **The navigations stay — there is nothing to fix in Phase 3.**

The one available resolution — flattening the entity POCOs to a single `eNote.Domain.Entities` namespace (folders unchanged; namespace declaration only) — is a **file-structure decision, not a code-logic one**, so it is **deferred to Phase 4**, alongside the rest of the folder-tree / file-organization pass. See Phase 4 backlog. Until then, ND1400 is a documented non-issue.

---

## 5. Sequencing & verification

**Order:** Workstream 1 (the real redesign) → Workstream 2 → Workstream 3. Separate commit per workstream; never batch.

**Per-commit gate:**
- `dotnet build eNote/eNote.sln` — 0 warnings / 0 errors.
- `dotnet test eNote/eNote.sln` — existing xUnit suite (billing, state machine, membership, validators) is the safety net.
- For WS1, add a focused test that `ICurrentActor` memoizes — resolve the student twice in one scope, assert a single DB hit.
- Re-run the NDepend project at the end to confirm the gate moved (informational, not the objective).

**Rollback:** `git revert` the offending commit; the workstream isolation guarantees a clean back-out.

**Then Phase 4 (style audit):** run it over the services Workstream 1 touched while they are fresh, so the redesign and the style pass compound instead of re-reading the same files twice.
