# Prompt: implement T10 and T12

Paste everything below this line into a fresh session (a different model, or a new
Claude session with no prior context on this project).

---

## Context

You are implementing two tasks in **eNoteV2**, a graded seminarski rad (Bosnian
university course project) with a .NET backend and two Flutter clients: `UI/enote_core`
(shared package, consumed by both desktop and a future mobile client) and
`UI/enote_desktop` (the desktop app, currently the only client built). Repo root:
`C:\Users\Tarik\Desktop\eNoteV2`. UI strings are in Bosnian. There is no mock framework
in this project — see `CLAUDE.md`'s "Service-interface policy" for how that constrains
interface use.

**Read `CLAUDE.md` first** (repo root) for the project's discovery/evidence/validation
conventions — use the tokensave/lurp indexed tools for code research before raw
grepping, label findings as observed fact vs. inference, and run only the narrowest
validation scope that actually proves each change.

Both tasks and their design decisions (D2, D3) are recorded in
`eNoteV2_Flutter_Audit_Remediation_Tasks_UPDATED.md` (repo root) — read T10, T12, and
§5's "D2 resolution" / "D3 resolution" blocks there for the full reasoning behind what
follows. The decisions are already made; your job is to implement them, not re-litigate
them.

## T10 — resolve the cancelled-lecture branch (decision: Option A)

**Files:**
- `UI/enote_desktop/lib/features/instructor/lecture/lecture_list_screen.dart`
- `UI/enote_desktop/lib/features/instructor/lecture/lecture_form_screen.dart`

**Change:**
1. In `lecture_list_screen.dart`'s `_openForm`, delete the early-return guard that shows
   an `ErrorBanner` and blocks navigation when `existing.isCancelled`. Tapping a
   cancelled lecture should push `LectureFormScreen` exactly like any other lecture.
2. In `lecture_form_screen.dart`, collapse the two separate field lists (the
   `_isCancelled` read-only `Scaffold` branch and the live `EntityFormScaffold` branch)
   into **one** field list, parameterized by an `enabled: !_isCancelled` flag (or
   equivalent) on each field, with validators/`onChanged` conditioned off when
   read-only. Keep the orange "Ovo predavanje je otkazano..." warning banner shown only
   when cancelled. Remove the now-redundant second `Scaffold`/field block entirely — one
   build path, not two.

**Acceptance:** exactly one field list in `lecture_form_screen.dart`; opening a
cancelled lecture from the list works and shows it read-only with the warning; opening a
live lecture still works exactly as before (editable, same validators).

**Validation:** `flutter analyze` on `enote_desktop`. Manually open a cancelled lecture
and a live lecture from the list screen and confirm the expected behaviour for each.
Run the existing test suite.

## T12 — drop `UserProvisionService.getById` (decision: drop it)

**Files:**
- `UI/enote_desktop/lib/features/admin/users/user_provision_service.dart`

**Change:** delete the `getById` method. If its `UserProfileResponse` import in this
file becomes unused as a result, remove that import too. **Do not** touch
`UserProfileResponse`/`UserProfile`/`UserAddressDto`/`UserIdentityDto` in
`enote_core` — they stay, per this tracker's §6 bucket-B list, for the future
`enote_mobile` client.

**Acceptance:** `getById` no longer exists on `UserProvisionService`; nothing else in
`enote_desktop` references it (confirm with a repo-wide search, not just this file);
`enote_core`'s `UserProfileResponse` model is untouched.

**Validation:** `flutter analyze` on `enote_desktop`. Run the existing test suite.

## Scope

Touch only the files named above. Do not expand into T9, T11, or any other tracker item.
Do not re-open D2/D3 — the decisions are settled; implement exactly what they specify.

## When done

Update `eNoteV2_Flutter_Audit_Remediation_Tasks_UPDATED.md`:
- Mark T10 and T12 ☑ in the §7 progress tracker table.
- Add a "Status: done" block under each task's own section (T10 is under §4; T12 has no
  dedicated section yet — add one there, or note completion in the D3 resolution block),
  following the style already used for T0–T8: what changed, files touched, validation run
  and its result.

Report back: what changed, files touched, validation run and result, per `CLAUDE.md`'s
"Completion reports" section.
