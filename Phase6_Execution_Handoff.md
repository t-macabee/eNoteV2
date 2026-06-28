# eNoteV2 — Phase 6: Execution Handoff for Qwen3:4b + Aider

**Source of truth:** [Phase6_Audit_Report.md](Phase6_Audit_Report.md)

---

## Manual fixes (do first, before Qwen execution)

### ApplicationServiceExtensions.cs — Lines 40–43
**Remove these four lines entirely:**
```csharp
services.AddScoped<ICourseAnnouncementService, AnnouncementService>();
services.AddScoped<IStoreAnnouncementService, AnnouncementService>();
services.AddScoped<IStudentAnnouncementService, AnnouncementService>();
services.AddScoped<IAdminInstructorService, AdminInstructorService>();
```

**Why:** Scrutor scan (lines 34–38) already registers all four via `AsImplementedInterfaces()`. These explicit lines are redundant.

After deletion, verify: `dotnet build eNote/eNote.sln` (0 warnings/errors).

---

## Execution model for Qwen3:4b

**File-by-file via Aider.** Each file = one Aider invocation. After each file:
```bash
dotnet build eNote/eNote.sln  # must be 0 warnings / 0 errors
dotnet test eNote/eNote.sln   # must pass 22/22
```

---

## Priority order for execution

### **Tier 0** (Correctness — must do first)
1. **RentalNotificationOutboxProcessor.cs** — **DELETE THE ENTIRE FILE**  
   *Why:* DUP-1 finding. Both `RentalNotificationOutboxProcessor` (Worker) and `RentalNotificationOutboxPublisher` (Infrastructure) process the same outbox table. Running both simultaneously causes duplicate MassTransit publishes. Remove the Worker version.
   
   Then remove the import line from Worker project startup if it's explicitly registered.

### **Tier 1** (High-ROI mechanical fixes)
2. **Passthrough async methods in RentalCommandService.cs** (lines 68–76)  
   Remove `async`/`await` from `ApproveAsync`, `RejectAsync`, `PickupAsync`, `CompleteAsync`, `ReturnEarlyAsync`.  
   Change to: `public Task<InstrumentRentalDto> ApproveAsync(...) => ExecuteStoreTransitionAsync(...);`

3. **AuthController.cs duplicates** (lines 63–72)  
   Extract `CurrentTokenJti` and `CurrentTokenExpiresAtUtc` into `CoreController` (they already exist there).  
   Delete from `AuthController`.  
   Make `AuthController` inherit from `CoreController` instead of `ControllerBase`.

4. **NotificationService.cs — inconsistent injection**  
   Replace `ICurrentUserService currentUserService` with `ICurrentActor actor`.  
   Change all `currentUserService.UserId` to `actor.UserId`.

### **Tier 2** (Sealed classes — easy wins)
5. **Add `sealed` to these classes:**
   - `eNote.API/Services/CurrentUserService.cs`
   - `eNote.API/Controllers/Auth/AuthController.cs`
   - `eNote.Infrastructure/Identity/TokenRevocationService.cs`
   - All Domain entity classes: `Course.cs`, `Lecture.cs`, `Student.cs`, `InstrumentRental.cs`, `Notification.cs`

### **Tier 3** (CancellationToken threading — BATCH)
6. **CancellationToken cross-cutting [BATCH]**  
   Add `CancellationToken ct = default` parameter and thread through EF Core calls in:
   - All `eNote.Application/Features/**/Services/*.cs` files listed in report
   - `eNote.Infrastructure/Identity/*.cs` (where framework allows)
   - Controller action methods (ASP.NET Core auto-binds from `HttpContext.RequestAborted`)
   
   **Start from controllers (outermost), then cascade down to services.** Controller pattern:
   ```csharp
   [HttpGet("{id}")]
   public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
   {
       var result = await _service.GetByIdAsync(id, cancellationToken);
       return Ok(result);
   }
   ```

### **Tier 4** (Other specific findings)
7. **ReferenceCrudController.cs** — Add generic constraint to `GetDtoId` (line 50)  
   Accept `Func<TDto, object>` ID extractor instead of reflection.

8. **SmtpEmailService.cs** — Add `readonly` to fields (lines 14–24)

9. **CurrentActor.cs** — Nullable int pattern fix (line 35)  
   Change `Select(x => x.MusicStoreId)` to `Select(x => (int?)x.MusicStoreId)`.

10. **RecommendationService.cs** — Extract magic numbers (lines 235, 236, 251)  
    Define `private const double RentalScoreWeight = 0.6;` etc.

11. **DevelopmentDataSeed.cs** — Use `IClock` instead of `DateTime.UtcNow` (line 32)

12. **RentalNotificationOutboxProcessor.cs** — Document duplicate detection limitation  
    Note: dedup on `UserId + RentalId + Title` is fragile; consider message correlation ID.

13. **Manual mapping in AssignmentSubmissionService.cs** (line 107–116)  
    Replace field-copy block with `mapper.Map<AssignmentSubmissionDto>(submission)` + name field injection.

14. **FileAccessService.cs** — Extract magic strings (lines 18–19)  
    Define `private const string AssignmentFilePath = "/api/uploads/assignments/";` etc.

15. **AuthService.cs** — String comparison (line 62)  
    Replace `error == Messages.UsernameTaken` with typed error discriminant or constant reference.

---

## Files with "Clean" status (skip)
Per the audit report, these files need no changes:
- `UserSelfService.cs`
- `StudentDisplayNameService.cs`
- `UserProfileLookup.cs`
- `PagingExtensions.cs`
- `SystemClock.cs`
- `ENoteContextFactory.cs`
- `TokenService.cs`
- Most controllers (already `sealed`, proper patterns)

---

## Guardrails for Qwen3:4b

1. **One file per Aider invocation.** Do not batch multiple files.
2. **Build gate after every file:**
   ```bash
   dotnet build eNote/eNote.sln
   dotnet test eNote/eNote.sln
   ```
   If build fails, **stop and report** — do not continue.
3. **Do not execute findings marked `[BATCH]`** in the report as individual per-file fixes. Wait for the batch instruction in Tier 3.
4. **Do not rename or restructure** — only add/remove/replace code.
5. **Commit after every 2–3 files** or after each Tier completes:
   ```bash
   git add .
   git commit -m "Phase 6: [Tier 1] passthrough async + auth consolidation"
   ```

---

## After Qwen finishes

1. Run full suite: `dotnet build` + `dotnet test` (must be 22/22 passing, 0 warnings)
2. Run NDepend again to confirm:
   - `sealed` classes now pass ND1203
   - Duplicates removed (DUP-1, DUP-2)
   - CancellationToken threading reduces latency on long operations
3. Commit the NDepend follow-up report as `Phase 6 complete — post-execution NDepend audit`

---

## Summary

**Total findings:** 48 (excluding CancellationToken, which is 60+ methods but counted as one cross-cutting batch)  
**Estimated execution time:** 3–4 hours (mostly CancellationToken threading)  
**Highest-ROI fixes:** DUP-1 (delete file) → Passthrough async (5 methods) → Sealed classes (9 classes) → CT threading (batch)
