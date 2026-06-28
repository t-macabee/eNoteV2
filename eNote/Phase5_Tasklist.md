# eNoteV2 — Phase 5: Execution Tasklist

**Source:** [Phase4_Audit_Report.md](Phase4_Audit_Report.md)  
**Gate per task:** `dotnet build eNote/eNote.sln` (0 warnings / 0 errors) + `dotnet test eNote/eNote.sln` (22/22)  
**One commit per task. Never batch.**

---

## Task 1 — Flatten entity sub-namespaces *(mechanical)*
**Finding:** #7

Change **only** the `namespace` declaration lines in the files below. Do not move files, do not touch folder structure, do not change EF configs.

**Files to change** (`namespace X.Y;` → `namespace eNote.Domain.Entities;`):

| File | Current namespace | Target |
|---|---|---|
| `eNote/eNote.Domain/Entities/Assignments/Assignment.cs` | `eNote.Domain.Entities.Assignments` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Assignments/AssignmentSubmission.cs` | `eNote.Domain.Entities.Assignments` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Communication/Announcement.cs` | `eNote.Domain.Entities.Communication` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Communication/Notification.cs` | `eNote.Domain.Entities.Communication` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Communication/RentalNotificationOutbox.cs` | `eNote.Domain.Entities.Communication` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Identity/Instructor.cs` | `eNote.Domain.Entities.Identity` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Identity/MusicStoreEmployee.cs` | `eNote.Domain.Entities.Identity` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Identity/RevokedToken.cs` | `eNote.Domain.Entities.Identity` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Identity/Student.cs` | `eNote.Domain.Entities.Identity` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Rentals/Instrument.cs` | `eNote.Domain.Entities.Rentals` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Rentals/InstrumentRental.cs` | `eNote.Domain.Entities.Rentals` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Rentals/InstrumentType.cs` | `eNote.Domain.Entities.Rentals` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Rentals/InstrumentView.cs` | `eNote.Domain.Entities.Rentals` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Rentals/MusicStore.cs` | `eNote.Domain.Entities.Rentals` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Shared/Address.cs` | `eNote.Domain.Entities.Shared` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Shared/Base/BaseEntity.cs` | `eNote.Domain.Entities.Shared.Base` | `eNote.Domain.Entities` |
| `eNote/eNote.Domain/Entities/Shared/Base/IEntity.cs` | `eNote.Domain.Entities.Shared.Base` | `eNote.Domain.Entities` |

**After changing namespace declarations:** the compiler will flag every file that has a `using eNote.Domain.Entities.Assignments;`, `using eNote.Domain.Entities.Identity;`, etc. Remove those `using` directives — they are now redundant (types are in the same flat namespace). Build errors are your guide; fix all of them before committing.

**Verify schema unchanged:**
```
dotnet ef migrations add _verify_noop --project eNote/eNote.Infrastructure --startup-project eNote/eNote.API
```
The migration must be **empty** (no Up/Down body). Discard it immediately: `dotnet ef migrations remove --project eNote/eNote.Infrastructure --startup-project eNote/eNote.API`

**Commit message:** `Flatten entity namespaces to eNote.Domain.Entities`

---

## Task 2 — Delete ghost folder `Features/Students/` *(mechanical)*
**Finding:** #8

1. Move `eNote/eNote.Application/Features/Students/StudentEnrollmentExtensions.cs` → `eNote/eNote.Application/Features/Academic/Courses/StudentEnrollmentExtensions.cs`
2. Change the namespace declaration in that file:  
   `namespace eNote.Application.Features.Students;` → `namespace eNote.Application.Features.Academic.Courses;`
3. Update any `using eNote.Application.Features.Students;` references across the solution.
4. Delete the now-empty `eNote/eNote.Application/Features/Students/` folder.

**Commit message:** `Move StudentEnrollmentExtensions into Academic.Courses, remove ghost folder`

---

## Task 3 — Standardize `ICurrentActor` method naming *(judgment)*
**Finding:** #3

`ICurrentActor` has inconsistent prefixes: `GetStudentAsync`, `GetInstructorAsync`, `GetActiveEmployeeAsync`, `GetCurrentStudentIdAsync`, `GetActiveStoreAsync`. Standardize to `GetCurrent*`:

| Old name | New name |
|---|---|
| `GetStudentAsync()` | `GetCurrentStudentAsync()` |
| `GetInstructorAsync()` | `GetCurrentInstructorAsync()` |
| `GetActiveEmployeeAsync()` | `GetCurrentEmployeeAsync()` |
| `GetCurrentStudentIdAsync()` | unchanged |
| `GetActiveStoreAsync()` | `GetCurrentStoreIdAsync()` |

**Files to update:**
- `eNote/eNote.Application/Common/Interfaces/ICurrentActor.cs` — rename interface methods
- `eNote/eNote.Application/Features/Identity/Users/Services/CurrentActor.cs` — rename implementations
- All consumers (search solution-wide for each old name): `RentalCommandService`, `RentalQueryService`, `AnnouncementService`, `ReportService`, `CourseService`, `CourseEnrollmentService`, `LectureService`, `LectureNoteService`, `LectureAttendanceService`, `AssignmentService`, `AssignmentSubmissionService`, `RankingService`, `InstrumentService`, `RecommendationService`, `InstructorAccessService`

**Do a solution-wide search for each old method name before committing to confirm zero remaining references.**

**Commit message:** `Standardize ICurrentActor method naming to GetCurrent* prefix`

---

## Task 4 — Unify `GetActiveStoreAsync` caching *(micro-fix)* 
**Finding:** #1

In `eNote/eNote.Application/Features/Identity/Users/Services/CurrentActor.cs`, replace the manual null-check pattern in `GetActiveStoreAsync` with `??=` consistent with the other three methods — **only if the zero-guard (storeId == 0 → exception) can be preserved cleanly.** If not, leave it as-is and skip this task.

One valid approach:
```csharp
public async Task<int> GetActiveStoreAsync()
{
    if (_storeId is not null) return _storeId.Value;
    var storeId = await context.Set<MusicStoreEmployee>()
        .AsNoTracking()
        .Where(x => x.AppUserId == user.UserId && x.IsActive)
        .Select(x => x.MusicStoreId)
        .SingleOrDefaultAsync();
    if (storeId == 0) throw new BusinessException(Messages.ActiveEmployeeStoreNotFound);
    return (_storeId = storeId).Value;
}
```
(The `??=` shorthand doesn't apply cleanly here because of the guard — the above is the closest consistent form.)

**Commit message:** `Tighten GetActiveStoreAsync caching pattern in CurrentActor`

---

## Task 5 — Extract duplicate announcement creation logic *(micro-fix)*
**Finding:** #6

In `eNote/eNote.Application/Features/Communication/Announcements/Services/AnnouncementService.cs`, extract the shared entity-construction logic from `CreateForCourseAsync` and `CreateForStoreAsync` into a private helper:

```csharp
private Announcement BuildAnnouncement(string title, string content, int? courseId, int? storeId) => new()
{
    Title = title,
    Content = content,
    CourseId = courseId,
    MusicStoreId = storeId,
    CreatedAt = clock.UtcNow
};
```

Call it from both create methods. Same pattern for the update methods if they share logic.

**Commit message:** `Extract shared announcement builder to eliminate duplication`

---

## Deferred / Do not touch

- **Finding #2** (`IUserProfileLookup` single-implementation) — defer until testability pressure emerges. YAGNI.
- **Finding #9** (`InstructorAccessService.GetInstructorAsync` proxy) — uncertain, requires caller audit first. Leave.
- **ND1212** (empty interface in Rentals) — pre-existing, not Phase 4 scope.
- **ND1500** (API Breaking Changes) — expected deletions from Phase 3, not a bug.
