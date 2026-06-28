# Phase 4 Style Audit — Findings

**Audit Date**: 2026-06-28  
**Phase 3 Commit**: `e1ab271` (Refactor current actor resolution and tighten report formatting)  
**Scope**: 6 Phase 3 service rewrites + 5 consumer spot-checks + DI registration & entity namespace structure

---

## Summary

**8 findings across 4 themes.** Estimated effort to address top 3: **low** (all non-breaking, mechanical fixes). Phase 3's actor redesign is sound; these are secondary cleanups unmasked by the refactor, not architectural defects.

**Key wins from Phase 3:**
- `RentalCommandService` dependency count reduced from 8 → 6 (removing `IUserContextResolver`, `IMusicStoreContextService`, `ICurrentUserService` noise)
- `UserContextResolver` correctly eliminated; its dual responsibilities split into `UserProfileLookup` (data queries) + `StudentDisplayNameService` (name formatting)
- `ICurrentActor` cleanly centralizes actor resolution with null-coalescing caches

---

## Findings (by theme, ranked by ROI)

### Verbosity

1. **Redundant null-coalescing cache in `CurrentActor.GetActiveStoreAsync`**  
   *File:* `eNote/eNote.Application/Features/Identity/Users/Services/CurrentActor.cs:25–39*  
   *Observation:* `GetActiveStoreAsync` manually implements the `??=` pattern (checking `_storeId is not null`), while the three entity-getter methods use inline `??=` (lines 20, 22, 23). → **Inconsistency in caching style. Recommendation:** Use `??=` for `_storeId` as well for visual consistency and brevity:
   ```csharp
   public async Task<int> GetActiveStoreAsync() => 
       _storeId ??= await context.Set<MusicStoreEmployee>()
           .AsNoTracking()
           .Where(x => x.AppUserId == user.UserId && x.IsActive)
           .Select(x => x.MusicStoreId)
           .SingleOrDefaultAsync();
   ```
   (Must guard against 0; alternative: use `int?` caching, or add a dedicated method.)

2. **`IUserProfileLookup` single implementation `UserProfileLookup` — inject concrete class or compose into CurrentActor**  
   *Files:* `IUserProfileLookup.cs`, `UserProfileLookup.cs`, `CurrentActor.cs:10`  
   *Observation:* `IUserProfileLookup` has one concrete class (`UserProfileLookup`), used only by `CurrentActor` (and transitively `InstructorAccessService` which wraps its methods). No external clients. The interface exists for testability/mocking, not polymorphism. → **Recommendation:** Either (a) inject `UserProfileLookup` directly into `CurrentActor` and remove the interface, or (b) compose the lookup methods inline into `CurrentActor` using a private helper. The current design adds one layer of indirection for zero behavior variation.

### Naming

3. **Asymmetric method naming on `ICurrentActor` for entity retrieval**  
   *File:* `eNote/eNote.Application/Common/Interfaces/ICurrentActor.cs:7–10`  
   *Observation:* Methods are named `GetStudentAsync()`, `GetInstructorAsync()`, `GetActiveEmployeeAsync()`, but ID versions are `GetCurrentStudentIdAsync()` (with "Current" prefix) and `GetActiveStoreAsync()` (with "Active" prefix, different adjective). → **Inconsistency:** Callers must remember whether they want "Current" or "Active" or neither. Recommendation:** Standardize to one pattern:
   - Option A (current-focused): `GetCurrentStudentAsync()`, `GetCurrentInstructorAsync()`, `GetCurrentEmployeeAsync()`, `GetCurrentStudentIdAsync()`, `GetCurrentStoreIdAsync()`
   - Option B (bare): `GetStudentAsync()`, `GetInstructorAsync()`, `GetEmployeeAsync()`, `GetStudentIdAsync()`, `GetStoreIdAsync()`
   - Option C (activity-focused): `GetActiveStudentAsync()`, `GetActiveInstructorAsync()`, etc.
   
   **Recommendation:** Choose Option A (`GetCurrent*`) because the interface is scoped to the authenticated user, making "current" the semantic default.

4. **`StudentDisplayNameService.FormatName` should be public or extracted to a helper extension**  
   *File:* `eNote/eNote.Application/Features/Identity/Users/Services/StudentDisplayNameService.cs:21–24`  
   *Observation:* `FormatName(UserIdentityDto user)` is private. If display-name formatting rules ever need to be reused (e.g., in a report, an admin list, a bulk export), this logic is unavailable. Currently only used here, but the coupling to name-rendering logic is a future friction point. → **Recommendation:** Move to a `public static string FormatUserDisplayName(UserIdentityDto user)` method in a shared utility (e.g., `DisplayNameExtensions` or `UserIdentityExtensions`), or keep private if the rule is truly this service's domain only.

### SOLID

5. **`ReportService` mixes PDF rendering with business logic (query, filtering, billing)**  
   *File:* `eNote/eNote.Application/Features/Reports/Services/ReportService.cs` (~189 lines)  
   *Observation:* Single class handles (a) instructor-access checks (`GetCurrentInstructorIdAsync`, `instructorAccess.GetOwnedLectureAsync`), (b) EF queries with complex includes (`InstrumentRental` with `Instrument`, `StudentProfile`), (c) billing calculations (`RentalBilling.ApplyBilling`), and (d) PDF generation (QuestPDF fluent builder). The class coordinates five collaborators: `IAppDbContext`, `IClock`, `IRankingService`, `IInstructorAccessService`, `ICurrentActor`, `IStudentDisplayNameService`. → **Recommendation:** Extract PDF layout logic into a separate `PdfReportBuilder` or `AttendanceReportBuilder` class, leaving `ReportService` as a facade that queries and assembles DTO, then delegates rendering. Low priority—rendering and query logic are tightly coupled by Bosnian locale/culture, so separation carries no present ROI unless PDF formats multiply.

6. **`AnnouncementService` enforces tri-partition but logic is duplicated across course/store paths**  
   *File:* `eNote/eNote.Application/Features/Communication/Announcements/Services/AnnouncementService.cs` (~195 lines)  
   *Observation:* Implements three interfaces (`ICourseAnnouncementService`, `IStoreAnnouncementService`, `IStudentAnnouncementService`), creating the appearance of three separate domains. However, `CreateForCourseAsync` and `CreateForStoreAsync` share identical internal logic (entity construction, audit fields, save). Similarly, `UpdateForCourseAsync` and `UpdateForStoreAsync` repeat the update pattern. DRY is violated; refactoring would be mechanical. → **Recommendation:** Extract a private `CreateAnnouncementAsync(string title, string content, int? courseId, int? storeId)` helper to eliminate duplication. Low priority—the duplication is 8–10 lines per pair, and the tri-interface design is intentional for SRP boundaries.

### Structure

7. **Entity sub-namespaces inconsistent — flatten all to `eNote.Domain.Entities` (B1 from Phase 3)**  
   *Files:* All `eNote/eNote.Domain/Entities/**/*.cs`  
   *Observation:* Full audit reveals a split state:
   - `Academic/` (5 files) → `eNote.Domain.Entities` ← **already flat, accidentally correct**
   - `Assignments/` (2 files) → `eNote.Domain.Entities.Assignments`
   - `Communication/` (3 files) → `eNote.Domain.Entities.Communication`
   - `Identity/` (4 files) → `eNote.Domain.Entities.Identity`
   - `Rentals/` (5 files) → `eNote.Domain.Entities.Rentals`
   - `Shared/` + `Shared/Base/` (3 files) → `eNote.Domain.Entities.Shared` / `eNote.Domain.Entities.Shared.Base`

   The Academic entities are already at the B1 target (`eNote.Domain.Entities`). Every other subfolder still uses a sub-namespace. The planned fix (Phase 3 §4, deferred to here): **change only the `namespace` declarations** in the 5 non-Academic subfolders to `namespace eNote.Domain.Entities;` — folders stay untouched, no schema change. Verify with `dotnet ef migrations add _verify_noop` (must be empty diff). This resolves ND1400 cycles and makes the entity graph one honest flat namespace, matching how the navs actually work (Student ↔ InstrumentRental, Student ↔ Enrollment ↔ Course ↔ Instructor — all cross-subfolder, all correct). **ROI: High.**

8. **`Features/Students/` is a ghost folder — one orphaned file in the wrong home**  
   *File:* `eNote/eNote.Application/Features/Students/StudentEnrollmentExtensions.cs`  
   *Observation:* The entire `Students/` feature folder contains a single file: `StudentEnrollmentExtensions.cs` (namespace `eNote.Application.Features.Students`). Enrollment is an academic concept (joining a course), not a student-identity concept. The Application layer already groups Courses, Lectures, LectureNotes, and Assignments under `Features/Academic/`. → **Recommendation:** Move `StudentEnrollmentExtensions.cs` to `Features/Academic/Courses/` and update its namespace to `eNote.Application.Features.Academic.Courses`. Delete the empty `Features/Students/` folder. **ROI: Medium** — removes a misleading folder that implies a feature domain that doesn't exist.

9. **[UNCERTAIN] `InstructorAccessService` proxies `IUserProfileLookup.GetInstructorAsync` without adding value**  
   *File:* `eNote/eNote.Application/Features/Identity/Instructors/InstructorAccessService.cs:15`  
   *Observation:* Line 15: `public Task<Instructor> GetInstructorAsync(int userId) => lookup.GetInstructorAsync(userId);` is a one-liner that delegates to `lookup`. The method is public and called in 4 places (AssignmentService line 37, LectureService, etc.). However, `IInstructorAccessService` already exposes `GetCurrentInstructorIdAsync`, which calls `GetInstructorAsync`. A caller could import both interfaces and call either. → **Observation, not yet a recommendation:** Verify whether `GetInstructorAsync` is truly needed on `IInstructorAccessService` or if all callers can be rewritten to call `GetCurrentInstructorIdAsync` + extract the ID. If `GetInstructorAsync` is only a stepping stone for `GetCurrentInstructorIdAsync`, consider removing it (simplification) or documenting its purpose. Requires caller audit before deciding.

10. **API Controllers and Application Features structural divergence (no-fix, by design)**  
    *Observation:* API Controllers are fully consistent (folder = namespace throughout, no issues). However, Controllers organize resources flat (`Controllers/Assignments/`, `Controllers/Courses/`) while Application groups them by domain (`Features/Academic/Assignments/`, `Features/Academic/Courses/`). This is acceptable architectural divergence — Controllers reflect HTTP resource hierarchy; Application reflects domain groupings. Not a finding, but documented for clarity.

---

## Not findings (and why)

- **ND1400 (namespace cycles via EF entity navs) and ND1004 (8-param constructors)** — already analyzed and triaged in Phase 2; Phase 3 partially addressed ND1004 in `RentalCommandService` (8 → 6 params). Remaining heavy constructors (`AnnouncementService`, others) are acceptable given their domain (courses + rentals + file storage is genuinely rich).
- **ND2700 (culture-aware parsing)** — Phase 2 flagged 2 issues; Phase 3 did not reintroduce culture-sensitive parsing. No new exposure.
- **ND1305 (namespaces with few types)** — the entity sub-namespace split contributes to this; resolves as a byproduct of the B1 flatten (Finding #7).
- **Visibility nits (ND1804, ND1807, ND1803)** — cosmetic, low priority, deferred per Phase 2.
- **Speculative abstractions** — No over-abstraction detected. Every interface has a clear reason (multi-impl, testability, or segregated client roles). YAGNI is observed.

---

## Recommendations Summary (Priority Order)

| Priority | Finding | Effort | Impact |
|---|---|---|---|
| **1** | Flatten entity sub-namespaces to `eNote.Domain.Entities` — change `namespace` declarations in Assignments, Communication, Identity, Rentals, Shared (Finding #7) | **Low** | **High** — resolves ND1400, ND1305, one honest namespace for the entity graph |
| **2** | Move `StudentEnrollmentExtensions.cs` → `Features/Academic/Courses/`; delete `Features/Students/` (Finding #8) | **Very Low** | **Medium** — removes a misleading ghost folder |
| **3** | Standardize method naming on `ICurrentActor` — choose one prefix (`GetCurrent*` recommended) (Finding #3) | **Low** | **Medium** — improves call-site discoverability |
| **4** | Resolve `IUserProfileLookup` single-implementation status (Finding #2) | **Low** | **Low** — YAGNI; defer unless testability pressure emerges |
| **5** | Unify `GetActiveStoreAsync` caching pattern with `??=` (Finding #1) | **Very Low** | **Very Low** — style consistency only |
| **6** | Extract private `CreateAnnouncementAsync` helper (Finding #6) | **Low** | **Low** — DRY improvement, no behavior change |
| **–** | Audit `InstructorAccessService.GetInstructorAsync` usage (Finding #9) | **Medium** | **Low** — requires caller survey, uncertain payoff |

---

## Conclusion

Phase 3 successfully decoupled identity resolution. The actor redesign is sound; no defects or regressions found. API Controllers are structurally clean (folder = namespace throughout). The two structural priorities are mechanical and low-risk: **flatten entity namespaces** (B1, resolves ND1400/ND1305) and **delete the ghost `Features/Students/` folder**. Everything else is style-level cleanup.
