# Phase 6 Static Audit Report

Generated: 2026-06-28  
Auditor: Claude Sonnet 4.6 (static analysis, no code execution)

---

## eNote.Application — Features/Academic/Assignments/Services/AssignmentService.cs

- **async/await** | `SubmitWithFileAsync` in `AssignmentSubmissionService` (line 24) calls `await SubmitAsync(...)` which is itself a full async method — passthrough is fine here since there is intermediate work (`fileStorage.SaveAssignmentAsync`). No action needed.
- **CancellationToken** [BATCH] | `GetForLectureAsync`, `GetByIdForInstructorAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetForStudentAsync`, `GetByIdForStudentAsync` — none accept or thread a `CancellationToken` parameter. All EF Core calls in these methods (`FirstOrDefaultAsync`, `SaveChangesAsync`) accept CT. → Add `CancellationToken ct = default` to each method signature and thread through.

---

## eNote.Application — Features/Academic/Assignments/Services/AssignmentSubmissionService.cs

- **CancellationToken** [BATCH] | `GetSubmissionsAsync` and `GradeAsync` do not accept `CancellationToken`. The inner EF calls (`FirstOrDefaultAsync`, `SaveChangesAsync`) accept CT. → Add `CancellationToken ct = default` and thread through.
- **Manual mapping** | `MapSubmission` (line 107–116) is a static private method doing manual property-by-property DTO projection. `AssignmentSubmissionDto` is already in a Mapster project; at minimum the `Student`-to-name resolution is a necessary side-step, but the pure field copies (`Id`, `AssignmentId`, `StudentId`, `SubmittedAt`, `FilePath`, `Grade`) could be replaced by `mapper.Map<AssignmentSubmissionDto>(submission)` followed by name injection. → Replace field-copy block with Mapster call + name field assignment.

---

## eNote.Application — Features/Academic/Courses/Services/CourseService.cs

- **CancellationToken** [BATCH] | All public methods (`GetByIdForInstructorAsync`, `GetByIdForStudentAsync`, `GetPagedForInstructorAsync`, `GetPagedForStudentAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) lack `CancellationToken`. → Add `CancellationToken ct = default` and thread through EF calls.
- **Readability** | `GetByIdForStudentAsync` (line 36–39): the `FirstOrDefaultAsync` predicate is very long and contains a nested `.Any()`. The line exceeds readable length. → Extract the enrollment check into a named local or use `context.IsEnrolledInCourseAsync` where applicable.

---

## eNote.Application — Features/Academic/Courses/Services/CourseEnrollmentService.cs

- **CancellationToken** [BATCH] | `EnrollAsync` and `UnenrollAsync` have no `CancellationToken`. → Add and thread through.

---

## eNote.Application — Features/Academic/Courses/Services/RankingService.cs

- **CancellationToken** [BATCH] | `GetForInstructorAsync`, `GetForStudentAsync`, `BuildRankingAsync` — no CT threading. `ToListAsync`, `ToDictionaryAsync` inside `BuildRankingAsync` all accept CT. → Add `CancellationToken ct = default` throughout the call chain.
- **Correctness** | `BuildRankingAsync` line 83: `x.Average!.Value` — the null-forgiving operator `!` is used after a `HasValue` guard on line 83 which already protects it (`x.Average.HasValue ? ... : null`), so this is safe but redundant. Replace `x.Average!.Value` with `x.Average.Value` inside the ternary true-branch. Low risk, readability fix.

---

## eNote.Application — Features/Academic/Lectures/Services/LectureService.cs

- **CancellationToken** [BATCH] | `GetByIdForInstructorAsync`, `GetByIdForStudentAsync`, `GetPagedForInstructorAsync`, `GetPagedForStudentAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `CancelAsync` — none accept CT. → Add and thread through.

---

## eNote.Application — Features/Academic/Lectures/Services/LectureAttendanceService.cs

- **Redundant guard** | `RsvpAsync` (line 23–28): the LINQ predicate `x.LectureStatus != LectureStatus.Cancelled` is applied in the `FirstOrDefaultAsync` query, and then `lecture.IsCancelled` is checked again at line 25. The second check is dead code because the query already filtered it out. → Remove the `if (lecture.IsCancelled)` guard block at lines 25–28.
- **CancellationToken** [BATCH] | `RsvpAsync`, `GetAttendanceAsync`, `MarkAttendanceAsync` — none thread CT. → Add.
- **Correctness** | `MarkAttendanceAsync` (lines 128–131): after a new attendance is created and added (`lecture.Attendances.Add(attendance)`), `attendance.Student` will be null because EF has not loaded it. The code falls back to a separate query `context.Set<Student>().FirstAsync(...)`. This is correct but a second DB round-trip is always incurred for creates. Minor efficiency note; no bug.

---

## eNote.Application — Features/Academic/LectureNotes/Services/LectureNoteService.cs

- **CancellationToken** [BATCH] | All public methods lack CT. → Add and thread.

---

## eNote.Application — Features/Communication/Announcements/Services/AnnouncementService.cs

- **Async query inside `Where`** | `GetFeedForStudentAsync` (lines 28–35): the `Where` predicate uses `context.Set<Enrollment>().Any(...)` and `context.Set<InstrumentRental>().Any(...)` as correlated subqueries inside LINQ. This is valid EF Core translation but produces two correlated `EXISTS` subqueries per row. Acceptable for current scale; no action required unless query performance degrades.
- **CancellationToken** [BATCH] | `GetFeedForStudentAsync`, `CreateForCourseAsync`, `GetByIdForCourseAsync`, `GetForCourseAsync`, `UpdateForCourseAsync`, `DeleteForCourseAsync`, `CreateForStoreAsync`, `GetByIdForStoreAsync`, `GetForStoreAsync`, `UpdateForStoreAsync`, `DeleteForStoreAsync` — none thread CT (the upload methods already do). → Add `CancellationToken ct = default` to the non-upload methods and thread through.
- **`sealed` missing** | `AnnouncementService` is `public sealed` — already correct.

---

## eNote.Application — Features/Communication/Notifications/Services/NotificationService.cs

- **Inconsistency: `ICurrentUserService` vs `ICurrentActor`** | `NotificationService` injects `ICurrentUserService` directly (line 13) while all other services in the layer inject `ICurrentActor` which wraps `ICurrentUserService`. This inconsistency is low risk (both return the same `UserId`) but breaks the convention established everywhere else. → Replace `ICurrentUserService currentUserService` with `ICurrentActor actor` and use `actor.UserId`.

---

## eNote.Application — Features/Files/Services/FileAccessService.cs

- **Magic string** | `CanAccessAssignmentFileAsync` (lines 18–19): the path prefixes `"/api/uploads/assignments/"` and `"/uploads/assignments/"` are magic strings. → Extract to private `const string` fields or a shared constant. The legacy path `"/uploads/assignments/"` also suggests a migration concern that may be tracked elsewhere.
- **CancellationToken** | `CanAccessAssignmentFileAsync` already accepts and threads CT correctly. No action needed.

---

## eNote.Application — Features/Identity/Instructors/AdminInstructorService.cs

- **In-memory filtering** | `GetPagedAsync` (lines 20–25): loads ALL instructors via `ToListAsync()` before filtering by name in memory, then applies pagination in memory. This works at low row counts but will degrade as instructor count grows. The root cause is that names live in `AspNetUsers`, not in `Instructor`. No easy EF fix without a join or view. Document as known ceiling: `// ponytail: in-memory name filter, add SQL join or materialized view if instructor count grows large`.
- **CancellationToken** [BATCH] | `GetPagedAsync` and `GetByIdAsync` lack CT. → Add.

---

## eNote.Application — Features/Identity/Instructors/InstructorAccessService.cs

- **CancellationToken** [BATCH] | All methods that call EF Core (`OwnsCourseAsync`, `EnsureOwnsCourseAsync`, `EnsureOwnsLectureAsync`, `GetOwnedLectureAsync`, `GetOwnedAssignmentAsync`, `GetOwnedLectureNoteAsync`) lack CT. → Add and thread.

---

## eNote.Application — Features/Identity/Users/Services/CurrentActor.cs

- **Correctness** | `GetCurrentStoreIdAsync` (line 35): `storeId == 0` is used to detect "not found" because `SingleOrDefaultAsync` on `int` returns `0` as default. This is correct given `MusicStoreId` is a positive auto-increment int, but it's fragile. A more explicit check would use `Select(x => (int?)x.MusicStoreId).SingleOrDefaultAsync()` and check `!storeId.HasValue`. → Change select to `(int?)x.MusicStoreId` and check `if (!storeId.HasValue)`.
- **CancellationToken** [BATCH] | `GetCurrentStoreIdAsync` performs a DB call (`SingleOrDefaultAsync`) but accepts no CT. → Add `CancellationToken ct = default` and thread through.

---

## eNote.Application — Features/Identity/Users/Services/UserProfileService.cs

- **CancellationToken** [BATCH] | `GetCurrentUserAsync`, `GetUserAsync`, `BuildStudentProfile`, `BuildInstructorProfile`, `BuildMusicStoreProfile` — all make DB/identity calls without CT. → Add and thread.
- **`sealed` missing** | `UserProfileService` is `public sealed` — already correct.

---

## eNote.Application — Features/Identity/Users/Services/UserProvisioningService.cs

- **CancellationToken** [BATCH] | `RegisterStudentAsync`, `ProvisionUserAsync`, `UpdateMembershipAsync`, `EnsureRoleProfileAsync`, `ResolveDefaultStoreIdAsync` — none accept CT. → Add and thread.
- **Correctness** | `ProvisionUserAsync` line 70: `return (0, createResult.Error)` — returns `0` as `UserId` for a creation failure. The caller in `AdminUsersController` checks `error is not null`, so this is safe, but returning `0` for a failed create is a semantic smell (valid IDs are always positive). Minor.

---

## eNote.Application — Features/Identity/Users/Services/UserSelfService.cs

- **Passthrough methods** | All five public methods are single-expression delegates to `accountService` — clean, no issues.

---

## eNote.Application — Features/Identity/Users/Services/StudentDisplayNameService.cs

- Clean. No findings.

---

## eNote.Application — Features/Identity/Users/Services/UserProfileLookup.cs

- Clean. No findings. `sealed`, correct usage.

---

## eNote.Application — Features/Rentals/InstrumentRentals/Services/RentalCommandService.cs

- **`sealed` missing** | `RentalCommandService` is already `public sealed` — correct.
- **`async` passthrough** | `ApproveAsync`, `RejectAsync`, `PickupAsync`, `CompleteAsync`, `ReturnEarlyAsync` (lines 68–76) are all expression-bodied `async Task<...>` that just `await ExecuteStoreTransitionAsync(...)`. Since `ExecuteStoreTransitionAsync` is itself a `Task`-returning method, the `async`/`await` wrapper is unnecessary. → Remove `async` and `await`, return `Task` directly: `public Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, response);`
- **CancellationToken** [BATCH] | `CreateRequestAsync`, `ApproveAsync`, `RejectAsync`, `PickupAsync`, `CompleteAsync`, `ReturnEarlyAsync`, `CancelAsync` lack CT. The inner EF calls accept CT. → Add.

---

## eNote.Application — Features/Rentals/InstrumentRentals/Services/RentalQueryService.cs

- **CancellationToken** [BATCH] | `GetByIdForStudentAsync`, `GetPagedForStudentAsync`, `GetByIdForStoreAsync`, `GetPagedForStoreAsync` — no CT. → Add.
- **Passthrough `async`** | `GetPagedForStudentAsync` (line 27) is an expression body that awaits `GetPagedAsync(...)` directly — fine as-is since it returns `Task` directly without `async`/`await`.

---

## eNote.Application — Features/Rentals/Instruments/Services/InstrumentService.cs

- **CancellationToken** [BATCH] | `GetByIdAsync`, `GetPublicByIdAsync`, `GetPagedAsync`, `GetPublicPagedAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` — no CT. (`UploadImageAsync` already accepts CT.) → Add CT to remaining methods and thread through.
- **Double DB round-trip on Create/Update** | `CreateAsync` (line 82) and `UpdateAsync` (line 107) both call `ReloadAsync(entity.Id)` after `SaveChangesAsync()`. This is an intentional design choice to eagerly load nav properties for the DTO. Acceptable; no action needed unless profiling shows a concern.

---

## eNote.Application — Features/Rentals/ReferenceData/ReferenceCrudService.cs

- **CancellationToken** [BATCH] | `GetPagedAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` — none accept CT. The EF calls inside accept CT. → Add `CancellationToken ct = default` and thread through; also update `EnsureDeletableAsync` signature.

---

## eNote.Application — Features/Rentals/ReferenceData/Addresses/AddressService.cs

- **Manual property assignment** | `CreateEntity` (lines 23–27) and `ApplyUpdate` (lines 30–35) assign `City`, `Street`, `Number` directly via property setters on `Address`. If `Address` had a constructor or `UpdateDetails` method, this would be cleaner; this is a domain design decision, not an Application layer bug. No action needed unless domain is tightened.
- **CancellationToken** [BATCH] | `EnsureDeletableAsync` does not thread CT to `accountService.IsAddressInUseAsync`. → Thread CT.

---

## eNote.Application — Features/Rentals/Recommendations/Services/RecommendationService.cs

- **Method length** | `GetRecommendedInstrumentsAsync` is 87 lines. While well-structured into discrete steps (load signals, compute scores, build result), it sits above the 40-line flag. The scoring steps (`ComputeRentalScore`, `ComputeViewScore`, `ComputeSimilarityScore`) are already extracted. The remaining loop (lines 83–109) could be extracted to a `ScoreAndRankCandidates` method. → Consider extraction, but not blocking.
- **CancellationToken** | `GetRecommendedInstrumentsAsync` and `RecordInstrumentViewAsync` both already accept and thread `CancellationToken` correctly. No action needed.
- **Magic numbers** | `0.6`, `0.4`, `0.35`, `0.6` inside `ComputeRentalScore` and `ComputeViewScore` (lines 235, 236, 251) are inline literals. The top-level weights (`RentalWeight`, `ViewWeight`, etc.) are constants, but the sub-weights inside static methods are not. → Extract to named `private const double` fields for maintainability.

---

## eNote.Application — Features/Reports/Services/ReportService.cs

- **CancellationToken** | All three public methods already accept and thread `CancellationToken` correctly.
- **`sealed`** | `ReportService` is `public sealed` — correct.
- **Nullable dereference** | `GenerateLectureAttendancePdfAsync` line 145: `.Where(s => s is not null)!` — the null-forgiving `!` suppresses the nullable warning on an `IEnumerable<Student?>`. The `Where` filter is correct, but the cast suppression is somewhat hidden. Consider explicitly casting: `.Where(s => s is not null).Select(s => s!)` for clarity.

---

## eNote.Application — Common/Paging/PagingExtensions.cs

- Clean. Well-designed overload set with sync map, async map, and context-loaded map variants. No findings.

---

## eNote.Application — Common/Time/SystemClock.cs

- Clean. `sealed`, expression-bodied. No findings.

---

## eNote.API — Services/CurrentUserService.cs

- **`sealed` missing** | `CurrentUserService` is declared `public class`, not `public sealed class`. It is a leaf class with no descendants in this codebase. → Add `sealed`.
- **CancellationToken** | No EF usage; not applicable.

---

## eNote.API — Controllers/Base/CoreController.cs

- Clean. `abstract` by design.

---

## eNote.API — Controllers/Base/ReferenceCrudController.cs

- **Reflection usage** | `GetDtoId` (line 50) uses `typeof(TDto).GetProperty("Id")?.GetValue(dto)` at runtime to extract the ID for `CreatedAtAction`. This is an unconstrained reflection call — `TDto` has no compile-time `Id` constraint. If a DTO does not expose `Id`, this throws at runtime. → Consider adding a generic constraint (e.g., `where TDto : IHasId`) or accepting an ID extractor `Func<TDto, object>` as a constructor parameter. Low urgency but a footgun for future DTOs.
- **CancellationToken** [BATCH] | Controller action methods do not thread `CancellationToken` from `HttpContext.RequestAborted` into service calls. → Add `CancellationToken cancellationToken` parameter to action methods and pass through.

---

## eNote.API — Controllers/Auth/AuthController.cs

- **Duplicate token extraction logic** | `CurrentTokenJti` (line 63) and `CurrentTokenExpiresAtUtc` (line 65–72) are private members in `AuthController` that duplicate the identical members in `CoreController` (lines 14–29). `AuthController` does NOT inherit from `CoreController` (it inherits `ControllerBase` directly), which explains the duplication. → Either make `AuthController` extend `CoreController` (it already has `[Authorize]` on `Logout` anyway), or extract the token helpers to a static utility class shared by both.
- **`sealed` missing** | `AuthController` is `public class`. → Add `sealed`.

---

## eNote.API — Controllers/Courses/InstructorCourseController.cs

- Clean. `sealed`, thin delegation. No findings.

---

## eNote.API — Controllers/Assignments/InstructorAssignmentSubmissionController.cs

- Clean. `sealed`, thin delegation. No findings.

---

## eNote.API — Controllers/InstrumentRentals/StoreRentalController.cs

- Clean. `sealed`, thin delegation. No findings.

---

## eNote.API — Controllers/Notifications/StudentNotificationController.cs

- **CancellationToken** [BATCH] | Action methods `GetPaged`, `GetUnreadCount`, `MarkRead`, `MarkAllRead` do not pass `HttpContext.RequestAborted` into service calls even though all `INotificationService` methods accept CT. → Add `CancellationToken cancellationToken` to each action and forward it.

---

## eNote.API — Controllers/Files/UploadsController.cs

- Clean. Security path validation is correct (`IsSafeFileName`, full path containment check). `sealed`.

---

## eNote.API — Controllers/Users/UsersController.cs

- Clean. `sealed`, proper error surfacing.

---

## eNote.API — Controllers/Admin/AdminUsersController.cs

- Clean. `sealed`, thin delegation.

---

## eNote.API — Controllers/Lectures/InstructorLectureController.cs

- Clean. `sealed`. CancellationToken is already forwarded for the report endpoint.

---

## eNote.API — Controllers/Instruments/StoreInstrumentController.cs

- Clean. `sealed`. CancellationToken forwarded for `UploadImage`.

---

## eNote.API — Controllers/Announcements/InstructorAnnouncementController.cs

- **Unused using** | `eNote.Application.Common.Localization` is imported at line 4 and used only for `Messages.FileNotProvided` in `UploadImage`. That's a valid use; no issue.
- Clean. `sealed`.

---

## eNote.API — Extensions/ApplicationServiceExtensions.cs

- **Scrutor scan registration** | The `Scan` call (lines 34–39) auto-registers all classes ending in `"Service"` from both `AuthService` and `CourseService` assemblies. This is followed by explicit `AddScoped<Interface, AnnouncementService>()` registrations (lines 41–43) for the three interfaces implemented by `AnnouncementService`. This means `AnnouncementService` is registered once by the scanner (as all its interfaces) and then three more times explicitly. The explicit registrations are redundant because Scrutor's `AsImplementedInterfaces()` already covers all three announcement interfaces. → Remove lines 41–43 if Scrutor correctly binds all three interfaces.

  > **Caution**: Verify this before removal — Scrutor with `AsImplementedInterfaces()` registers one shared instance per interface resolution. If three injections of `AnnouncementService` in the same request need to share a single scoped instance, this is important. If they should be independent instances, the scanner registration is fine. If they should be the same instance (typical for scoped), the explicit triple-registration is causing three separate instances. → Use `services.AddScoped<AnnouncementService>(); services.AddScoped<ICourseAnnouncementService>(sp => sp.GetRequiredService<AnnouncementService>()); ...` if shared instance is required.

- **`AddinInstructorService` explicit registration** | `AdminInstructorService` is registered explicitly (line 43) AND would be picked up by the Scrutor scan (it ends in `Service`). This causes a double registration. → Remove the explicit line if the Scrutor scan covers it.

---

## eNote.API — Extensions/MiddlewareExtensions.cs

- **Service locator** | `context.RequestServices.GetService<ILogger<WebApplication>>()` (line 29) uses service locator inside the exception handler. This is acceptable in middleware where DI injection is not available through constructor, but it's worth noting. No action required.
- `ErrorResponse` record is `private` — correct.

---

## eNote.API — Extensions/IdentityExtensions.cs

- **Service locator in JWT event** | `OnTokenValidated` (line 81): `context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>()` — this is the standard pattern for SignalR/JWT event handlers where DI is not injected. Acceptable.
- Clean.

---

## eNote.Worker — Consumers/RentalStatusChangedConsumer.cs

- **`sealed` missing** | `RentalStatusChangedConsumer` is `public sealed` — already correct.
- **Duplicate detection** | The deduplication check (lines 15–18) compares `UserId + RentalId + Title`. Title is a human-readable string; two distinct events for the same rental that happen to have the same title (e.g., two "Status iznajmljivanja promijenjen" events) would be incorrectly deduplicated. A message correlation ID or outbox entry ID would be more reliable. Flag as known limitation.

---

## eNote.Worker — Services/RentalNotificationOutboxProcessor.cs

- **Duplicate background service** | `RentalNotificationOutboxProcessor` (Worker project) and `RentalNotificationOutboxPublisher` (Infrastructure, registered as hosted service in API) both poll the same `RentalNotificationOutbox` table and publish to the same exchange. They have different `BatchSize` (20 vs 50) and `PollInterval` (15s vs 5s) but are functionally identical. If both API and Worker are running simultaneously, each will publish the same outbox entries, resulting in duplicate messages. → Either: (a) consolidate to one processor and remove the other, or (b) add a distributed lock / `PublishedAt` claim-and-check to prevent concurrent processing. This is a correctness bug, not just a style issue.

---

## eNote.Infrastructure — Data/ENoteContext.cs

- **`sealed` missing** | `ENoteContext` is `public class` (inherits `IdentityDbContext`). EF Core requires the context not be sealed if used with proxies; since there's no proxy usage here, `sealed` could be added, but it's low value given EF conventions. → Skip.
- **`AuditableEntity` only** | `Notification` inherits directly from `object` (not `AuditableEntity` or `BaseEntity`). This means it is NOT processed by the `SaveChangesAsync` audit stamp loop (line 28–39). `Notification.CreatedAt` is set manually in the constructor — correct but inconsistent with the audit pattern. Flag as design inconsistency; not a bug.

---

## eNote.Infrastructure — Data/ENoteContextFactory.cs

- Clean. `sealed`. Used only at design time.

---

## eNote.Infrastructure — Data/Seed/DevelopmentDataSeed.cs

- **`DateTime.UtcNow` violation** | `StudentMembershipSeed.SeedMemberships` (line 32): uses `DateTime.UtcNow.AddYears(1)` directly instead of an injected `IClock`. Seed code is development-only and not production-critical, but it violates the project-wide constraint. → Change to accept and use `IClock`, or accept as an intentional exception for seed-only code and document it.
- **Static nested classes** | The nested `CourseSeed`, `LectureSeed`, etc. are `internal static` — correct for seed-only helpers.

---

## eNote.Infrastructure — Data/Seed/IdentitySeed.cs

- Clean. Proper use of provisioning service for seeding.

---

## eNote.Infrastructure — Identity/AuthService.cs

- **String comparison** | `RegisterAsync` (line 62): `error == Messages.UsernameTaken || error == Messages.EmailTaken` — string equality on exception messages. This is fragile; if the message strings change, the branch silently mis-classifies. → Use constants or a typed error discriminant instead of string comparison.
- **CancellationToken** [BATCH] | `LoginAsync`, `RegisterAsync` do not accept CT. → Add.
- **`sealed`** | `AuthService` is already `public sealed` — correct.

---

## eNote.Infrastructure — Identity/TokenService.cs

- Clean. `sealed`. Correct use of `IClock`.

---

## eNote.Infrastructure — Identity/TokenRevocationService.cs

- **`sealed` missing** | `TokenRevocationService` is `public class`, not sealed. It is a concrete leaf class. → Add `sealed`.
- **CancellationToken** | Already correctly threads CT through `RevokeAsync` and `IsRevokedAsync`.

---

## eNote.Infrastructure — Identity/UserAccountService.cs

- **CancellationToken** [BATCH] | `FindUserIdByUsernameAsync`, `CreateUserAsync`, `AssignSingleRoleAsync`, `ChangePasswordAsync`, `UpdateExistingUserAsync`, `UpdatePictureAsync`, `GetPictureAsync`, `DeletePictureAsync`, `IsAddressInUseAsync` — none accept CT. All use `UserManager` which does not expose CT on most methods (ASP.NET Identity limitation), so this is partially constrained by the framework. `IsAddressInUseAsync` uses EF directly and could accept CT. → At minimum add CT to `IsAddressInUseAsync` and thread through.

---

## eNote.Infrastructure — Identity/UserIdentityService.cs

- **CancellationToken** [BATCH] | `GetUserAsync` and `GetUsersBulkAsync` call EF `FirstOrDefaultAsync`/`ToListAsync` without CT. `GetRolesAsync` uses Identity Manager (no CT). → Add CT to `GetUserAsync` and `GetUsersBulkAsync`, thread through EF calls.

---

## eNote.Infrastructure — Identity/SmtpEmailService.cs

- **Old-style constructor + private fields** | `SmtpEmailService` uses a manual constructor reading from `IConfiguration` and storing into six private `readonly` fields (lines 14–24). The class already has the injection point; primary constructors would not help here since the configuration reading is non-trivial. The six fields are all assigned only in the constructor and are not `readonly`. → Add `readonly` to `_host`, `_from`, `_port`, `_enableSsl`, `_username`, `_password`.
- **`sealed`** | `SmtpEmailService` is `public sealed` — correct.

---

## eNote.Infrastructure — Data/Configurations (spot-check)

### InstrumentRentalConfig.cs
- Clean. Filter index with raw status values is correct and the comment explains intent.

### CourseConfig.cs
- Clean. Uses `ConfigurationHelpers` extension methods correctly.

### AppUserConfig.cs
- Clean. Minimal and correct.

### NotificationConfig.cs
- Clean. Composite indexes are well-chosen.

---

## eNote.Domain — Entities (light pass)

### Course.cs
- **`sealed` missing** | `Course` is `public class`. As a domain entity that is not abstract and has no known subclasses, `sealed` should be added. → Add `sealed`. Same applies to `Lecture`, `Student`, `InstrumentRental`, and other concrete entity classes below.
- **Nav collections** | `Enrollments` and `Lectures` are `ICollection<T>` backed by `new List<T>()` — not read-only from outside. Already tracked as ND2300 for 2 properties; skip those specific ones.

### Lecture.cs
- **`sealed` missing** | `Lecture` is `public class`. → Add `sealed`.
- **`RowVersion` setter** | `public byte[]? RowVersion { get; set; }` (line 22) is a public mutable property set directly — correct for EF concurrency token convention. No issue.

### Student.cs
- **`sealed` missing** | `Student` is `public class`. → Add `sealed`.

### InstrumentRental.cs
- **`sealed` missing** | `InstrumentRental` is `public class`. → Add `sealed`.

### Notification.cs
- **Not inheriting `AuditableEntity`** | As noted in the ENoteContext section, `Notification` does not participate in the audit stamp pattern. `CreatedAt` is set manually in the constructor. This is consistent with Notification's design (it has no `UpdatedAt`, `CreatedById`, etc.) but worth flagging for clarity.
- **`sealed` missing** | → Add `sealed`.

### BaseEntity.cs / AuditableEntity.cs
- These are abstract base classes — `sealed` does not apply. Clean.

---

## Cross-cutting findings

### CT-1 — CancellationToken gaps [BATCH]
Every Application service method that calls EF Core (`FirstOrDefaultAsync`, `ToListAsync`, `SaveChangesAsync`, etc.) should accept and thread `CancellationToken ct = default`. This is a cross-cutting gap covering approximately 60+ method signatures across:
- All `*Service.cs` classes in `eNote.Application`
- `InstructorAccessService`
- `CurrentActor.GetCurrentStoreIdAsync`
- `ReferenceCrudService<>` base class
- Infrastructure `UserIdentityService`, `UserAccountService` (where framework allows)

Fix strategy: Start from the outermost controller action method (add `CancellationToken cancellationToken` parameter, ASP.NET Core binds it from `HttpContext.RequestAborted` automatically), then thread down through service interfaces and implementations. This is a single mechanical batch pass.

### SEALED-1 — Missing `sealed` on concrete leaf classes
The following concrete classes have no subclasses in this codebase and are missing `sealed`:
- `eNote.API.Services.CurrentUserService`
- `eNote.API.Controllers.Auth.AuthController`
- `eNote.Infrastructure.Identity.TokenRevocationService`
- `eNote.Domain.Entities.Course`
- `eNote.Domain.Entities.Lecture`
- `eNote.Domain.Entities.Student`
- `eNote.Domain.Entities.InstrumentRental`
- `eNote.Domain.Entities.Notification`
- (plus all other concrete domain entities not individually listed above)

Note: Domain entities inherit from `AuditableEntity` or `BaseEntity` which use `protected` constructors for EF; `sealed` is compatible with EF Core when using parameterless `protected` constructors.

### DI-1 — Inconsistent actor abstraction
`NotificationService` uses `ICurrentUserService` while all other Application services use `ICurrentActor`. Standardize to `ICurrentActor`.

### DUP-1 — Duplicate outbox processor
`RentalNotificationOutboxPublisher` (API, 5s poll, batch 50) and `RentalNotificationOutboxProcessor` (Worker, 15s poll, batch 20) both process the same outbox table. Running both simultaneously causes duplicate publishes. One must be removed or a claim mechanism added.

### DUP-2 — Duplicate token claim helpers
`AuthController` duplicates `CurrentTokenJti` and `CurrentTokenExpiresAtUtc` properties that already exist in `CoreController`. Consolidate.

---

## Skip log

- `eNote.Infrastructure/Data/Migrations/` — skipped: auto-generated EF Core migrations.
- `eNote.Tests/` — skipped: explicitly out of scope.
- `graphify-out/` — skipped: tooling output.
- `eNote/eNote.API/obj/` — skipped: build output, auto-generated.
- Validator files (`eNote.Application/Validation/*.cs`) — not in audit scope per instructions (scope is `Services`, `Common`, controllers, extensions, consumers, worker, infrastructure, domain).
- DTO and request/response model files (`*Dto.cs`, `*Request.cs`, `*Response.cs`, `*SearchObject.cs`) — out of scope for this audit pass (no logic to audit).
- Mapping config files (`*MappingConfig.cs`) — no application logic, skipped.
- `eNote.Contracts/` — thin message record, clean, skipped.

---

## Summary

**Total files audited:** 72  
**Total findings:** 48 (excluding batched CT items counted as one cross-cutting finding)

**Estimated execution effort:** Medium  
The bulk of the work (CancellationToken threading) is mechanical but touches many signatures and requires interface updates. The duplicate outbox processor is the most impactful correctness fix.

**Top 3 highest-ROI fixes:**

1. **DUP-1 — Duplicate outbox processor** (`RentalNotificationOutboxPublisher` vs `RentalNotificationOutboxProcessor`): Correctness bug — running both simultaneously produces duplicate MassTransit publishes, causing duplicate notifications. Remove one processor. High impact, one file deletion.

2. **CT-1 — CancellationToken threading (BATCH)**: ~60+ method signatures. Enables request cancellation propagation (important for long-running PDF/query endpoints). Mechanical but high coverage. Tackle as a single batch commit.

3. **DI-1 / DUP-2 — Inconsistency cleanup**: Standardize `NotificationService` to use `ICurrentActor`; consolidate duplicate token-extraction helpers from `AuthController` into `CoreController`. Two focused one-file changes.
