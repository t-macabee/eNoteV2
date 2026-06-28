# eNoteV2 — Backend compliance fixes (Razvoj softvera II)

.NET 10 / C# 13 modular monolith (API + Worker), EF Core 10, ASP.NET Identity, MassTransit/RabbitMQ, QuestPDF. Three fixes from a requirements audit. Work one task at a time; gate each with build + test before moving on.

## Gate (run after EACH task)
```
dotnet build eNote/eNote.sln    # 0 warnings / 0 errors
dotnet test eNote/eNote.sln     # all tests pass
```
If a task fails the gate, stop and report — do not continue to the next.

---

## Task 1 — File upload: validate magic bytes, not just MIME (REAL FIX)

Requirement: "File upload mora validirati MIME tip i magic bytes, a ne samo ekstenziju."

Read `eNote/eNote.Infrastructure/Storage/LocalFileStorageService.cs`. It currently validates only the client-supplied `contentType` string against allowlists (`AllowedImageContentTypes`, `AllowedAssignmentContentTypes`). That header is spoofable — a renamed executable sent as `image/png` passes.

Add real file-signature (magic-byte) validation to both upload paths (image save + assignment save):
- JPEG: `FF D8 FF`
- PNG:  `89 50 4E 47 0D 0A 1A 0A`
- WebP: `52 49 46 46` at offset 0 AND `57 45 42 50` at offset 8 (RIFF....WEBP)
- PDF:  `25 50 44 46` (%PDF)

Rules:
- Reject if the actual signature is not in the allowlist for that upload kind, throwing the project's existing validation/business exception (match how the current contentType rejection throws — read the file to see which exception + message convention it uses; reuse `Messages.*` localization if present).
- DO NOT advance the upload stream permanently — read the leading bytes, then reset `Position = 0` (or read into a buffer) so the subsequent save writes the full file.
- DRY: `eNote/eNote.Infrastructure/Identity/UserAccountService.cs:249` already has a `DetectContentType(byte[])` byte-sniffer. Prefer extracting/reusing one shared signature detector over duplicating the logic. If you extract it, put it somewhere both services can use and update both call sites.

Keep it minimal — a small signature lookup, no new library.

---

## Task 2 — Verify SignalR hub authorizes per-user (VERIFY, fix only if missing)

Requirement: real-time endpoints must verify the connected user is a legitimate participant — a student must not be able to receive another user's notifications.

Find and read `NotificationHub` (mapped in `eNote/eNote.API/Program.cs` via `MapHub`). Confirm:
- The hub class has `[Authorize]`.
- It scopes notifications to the caller's own user id taken FROM THE JWT (e.g. groups keyed by the authenticated user id), never from a client-supplied parameter.

If it already does this, report "compliant, no change." If a client could subscribe to another user's stream, fix it to bind the connection to the JWT user id only. Do not redesign the hub — smallest correct change.

---

## Task 3 — Verify MassTransit retry uses exponential backoff (VERIFY, fix only if missing)

Requirement (Appendix A.1): failed message processing must retry with exponential backoff (e.g. 1s → 2s → 4s → 8s), not silently swallow errors.

Read `eNote/eNote.Infrastructure/Messaging/MassTransitServiceExtensions.cs`. Confirm the bus/endpoint configuration calls `UseMessageRetry` with an incremental or exponential interval policy. If retry is absent or fixed-interval, add:
```
cfg.UseMessageRetry(r => r.Exponential(4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
```
(applied at the bus or receive-endpoint level, matching the existing config style). If already present and exponential/incremental, report "compliant, no change."

---

## Guardrails
- No speculative abstractions, no new dependencies. Smallest change that satisfies each requirement.
- Match existing exception types, localization (`Messages.*`), and async/CancellationToken conventions already in each file.
- One commit per task:
  - "Validate upload file signatures (magic bytes) in addition to MIME"
  - "Scope NotificationHub to authenticated user" (only if changed)
  - "Add exponential retry backoff to MassTransit consumers" (only if changed)
