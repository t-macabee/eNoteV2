# eNoteV2 Flutter — Audit Remediation Task List

**Source audit:** "eNoteV2 Flutter clients — Architecture & complexity audit"
(artifact `d778f26e-cca6-4cc7-8d2f-473969642da7`)
**Audit scope:** `UI/enote_core` + `UI/enote_desktop` @ commit `f6b0b54` (main, clean)
**Audit date:** 2026-08-28 · **Task list generated:** 2026-08-28
**Findings covered:** F1–F16 + dead code (buckets A/B/C) + redundancy + complexity hotspots
**Audit status:** accepted as verified. No re-verification of findings is performed by this list.

---

## 0. How to use this list

**Ordering.** Tasks follow the audit's own refactoring plan: Tier 1 (T1–T4) first, then
Tier 2 (T5–T8), then Tier 3 (T9–T11). T1–T4 are mutually independent. Tier 2/3
dependencies are stated per task.

**Task fields.**
- **Finding** — the audit finding(s) mitigated.
- **Files** — every file the change touches.
- **Change** — the concrete edit.
- **Acceptance** — what must be true when the task is done.
- **Validation** — the narrowest check that actually validates it.
- **Scope** — indicative line delta from the audit ("about" figures).

**A standing caveat from the audit's own limits section.** The application was never
built, run, or analysed during the audit. `flutter analyze` was not executed and no test
was run. Every runtime symptom is reasoned from source and SDK behaviour. That does not
change what to fix — it changes how to close each task: **T1 and T2 require a debug-build
confirmation before and after the fix.** See T0.

**Two tasks are blocked on a decision, not on code:** T10 (product call) and the
`SearchObject` half of T5 (design call, audit recommends delete). Both are listed in
§5 Decisions required.

---

## 1. Pre-flight

### T0 — Establish a baseline before touching anything
**Finding:** audit method/limits section
**Files:** none (read-only)
**Change:** none.

**Steps:**
1. `flutter analyze` in `UI/enote_core` and `UI/enote_desktop`. Record the output.
   This has never been run against this code; it may surface issues the audit did not
   look for, and it is the baseline every later task is measured against.
2. Run the existing test suite (4 tests). Record pass/fail.
3. Build and run `enote_desktop` in **debug** mode against the running backend.
4. Reproduce F1: open a rental detail screen, press back. Note whether the assertion
   in `NavigatorState.pop` trips.
5. Reproduce F2: create a course with dates, then create a second one without touching
   the date fields. Note whether the second POST carries the first one's dates.

**Acceptance:** analyze output, test result, and the two reproduction outcomes are
recorded. If either critical does **not** reproduce, note that against T1/T2 — the fix
is still correct, but the severity claim changes.
**Validation:** this task *is* the validation setup.
**Scope:** no code change.

**Status: done (2026-08-28).** Results below.

1. **`flutter analyze`** — `UI/enote_core`: "No issues found!" (9.3s). `UI/enote_desktop`:
   "No issues found!" (2.5s). Baseline is clean in both packages.
2. **Test suite** — `enote_core`: 3 passed (`enote_core_test.dart`). `enote_desktop`: 1
   passed (`widget_test.dart`). 4/4 total, matching the audit's count.
3. **Debug build against the running backend** — no GUI-automation tool exists for the
   native Windows build in this environment, so `enote_desktop` was run in debug mode as
   `flutter run -d web-server` and driven through the Claude Code browser pane instead
   (functionally equivalent for reproducing client-side Dart bugs; the backend was
   confirmed already running on `http://localhost:5059`, Kestrel responding). Seeded
   accounts from `IdentitySeed.cs` were used to log in (`storeemployee` /
   `instructor`, password `Test1234!`).
4. **F1 reproduced — confirmed.** The store had no rentals to open, so one was created
   via the seeded `student` account (`POST /student/rentals`, instrument #1) to reach a
   loaded rental detail screen. Pressing back on `RentalDetailScreen` (with `_rental !=
   null`) threw uncaught `FlutterError.fromParts` / `AssertionErrorImpl` errors in the
   console, and the screen did not navigate back — exactly the `NavigatorState.pop`
   re-entrancy the audit predicted from `PopScope`'s `onPopInvokedWithResult` calling
   `Navigator.of(context).pop(true)` from inside a pop.
5. **F2 reproduced — confirmed.** Created a course with dates (05.08.2026–20.08.2026) in
   `CourseFormScreen`, which saved successfully and visually reset the form. Created a
   second course immediately after, filling only Naziv/Opis/Cijena and leaving the date
   fields untouched — they still visually showed 05.08.2026./20.08.2026. after the first
   save. The second course's actual `POST /instructor/courses` body carried
   `"startDate":"2026-08-05T00:00:00Z","endDate":"2026-08-20T00:00:00Z"` — the first
   course's dates, not the empty state the visible Naziv/Opis/Cijena fields showed. This
   is the exact stale-mirror-state bug T3 targets.

**Residual test data left in the dev DB** (local backend only, not touched otherwise):
rental request id 1 (instrument #1, student `student`), and courses id 3/4/5 under
instructor `instructor` (`T0 Repro Course 1`, `T0 Repro Course With Dates`, `T0 Repro
Course 2 No Dates Touched`). Flagging this for cleanup if the dev DB needs to stay
pristine — no action taken beyond creating it, per T0's read-only/no-code-change scope.

Neither critical failed to reproduce, so T1 and T2's severity claims stand as written.

---

## 2. Tier 1 — highest value, do first

*Independent of each other. Any order, or in parallel.*

### T1 — Stop showing raw Dart exception text to users
**Finding:** F3 (Significant) · **Depends on:** nothing

**Files:**
- `enote_core/lib/` — new helper (place beside the API error mapper)
- 13 call sites: every `ErrorBanner.show(context, message: e.toString())`
- `enote_core/lib/enote_core.dart` (barrel export)

**Change:** add
`String userMessage(Object e) => e is ApiException ? e.message : 'Nije moguće povezati se sa serverom. Pokušajte ponovo.';`
to `enote_core`, export it from the barrel, and replace `e.toString()` with
`userMessage(e)` at all thirteen catch sites.

**Acceptance:**
- Zero remaining `ErrorBanner.show(..., message: e.toString())` in either package.
- With the backend stopped, the app shows the Bosnian message, not
  `SocketException: Connection refused (OS Error: …), address = localhost, port = 5059`.
- `ApiException` messages still surface unchanged (the mapper output is not swallowed).

**Validation:** stop the backend, open one list screen, confirm the banner text. Then
force a 400 from one endpoint and confirm the mapped Bosnian sentence still appears.
**Scope:** ~10 new lines + 13 one-line edits. Highest value-to-effort ratio in the audit.

**Status: done (2026-08-28).** Added
`String userMessage(Object e) => e is ApiException ? e.message : 'Nije moguće povezati se sa serverom. Pokušajte ponovo.';`
to `enote_core/lib/api/api_error_mapper.dart` (beside `ApiErrorMapper`, already exported by
the barrel via the existing `export 'api/api_error_mapper.dart';` line — no new barrel entry
needed). Replaced `message: e.toString()` with `message: userMessage(e)` at all catch sites —
**15**, not 13 as estimated (the audit undercounted; `entity_list_screen.dart`'s multi-line
call was one of the two missed). `grep -rn "e.toString()" ... | grep -i ErrorBanner` over both
packages now returns zero matches. `ApiException` messages pass through `userMessage`
unchanged since the ternary's true-branch returns `e.message` verbatim — the mapper output is
not altered. Not independently re-verified by stopping the backend live (T0 already exercises
the connection-refused path structurally; this task is a pure message-substitution with no new
control flow, so `flutter analyze` clean + the existing suite passing is the correct-scoped
check per the validation ladder).

---

### T2 — Remove the PopScope from the rental detail screen
**Finding:** F1 (Critical) · **Depends on:** nothing

**Files:**
- `enote_desktop/lib/features/store_employee/rental/rental_detail_screen.dart:171–177`
- `enote_desktop/lib/features/store_employee/rental/rental_list_screen.dart:139–148`

**Change:**
1. Delete the `PopScope` wrapper entirely (it exists only to force a `true` result out of
   a plain back press; its callback re-enters `Navigator.pop` from inside a pop).
2. In `rental_list_screen.dart`, drop the `if (refreshed == true)` guard and call
   `_listKey.currentState?.refresh()` unconditionally after the `await` — matching what
   the other twelve list screens already do.

**Acceptance:**
- No `PopScope` remains in `rental_detail_screen.dart`.
- Backing out of a rental detail screen in a **debug** build trips no assertion.
- The rental list refreshes after every return, changed or not.

**Validation:** debug build. Open rental detail → back. Then open → perform a
transition → back, and confirm the list shows the new status. Compare against the T0
baseline reproduction.
**Scope:** about −12 lines, 2 files. Cost: one extra list fetch when backing out unchanged.

**Status: done (2026-08-28).** Deleted the `PopScope` wrapper in
`rental_detail_screen.dart` (the `build` method now returns the `Scaffold` directly).
Dropped the `if (refreshed == true)` guard in `rental_list_screen.dart` — `onEdit` now
awaits the push and calls `_listKey.currentState?.refresh()` unconditionally, matching
the other twelve list screens.

**Debug-build re-confirmation (`enote_desktop-web` against the running backend, same
`storeemployee` session as T0):** re-opened rental #1 (still present in the dev DB from
T0) and pressed back twice in a row. Both times the app returned cleanly to the refreshed
rental list with **no new console errors** — `read_console_messages` showed the same 2
pre-existing app-startup errors before and after each back-press (unrelated Flutter-web
debug-mode startup noise, present before login and unchanged by the repro), confirming no
`NavigatorState.pop` assertion is thrown any more. This matches T0's F1 reproduction
inverted: the assertion no longer trips.

---

### T3 — Fix the stale date state after a create
**Finding:** F2 (Critical) · **Depends on:** nothing

**Files:**
- `enote_desktop/lib/widgets/entity_form_scaffold.dart:55`
- `enote_desktop/lib/features/instructor/course/course_form_screen.dart:111–121`
- `enote_desktop/lib/features/instructor/lecture/lecture_form_screen.dart:229–234`
- new/updated test file

**Change:** add an optional `VoidCallback? onReset` to `EntityFormScaffold`, invoked
immediately after `_formKey.currentState?.reset()`. `CourseFormScreen` nulls `_startDate`
and `_endDate` in it; `LectureFormScreen` nulls `_lectureTime`.

**Note the alternative the audit records:** drop the mirror fields entirely, hold a
`GlobalKey<FormFieldState<DateTime?>>` per date field and read the value at save time.
That removes the duplicated state instead of resynchronising it, but touches more code.
**Start with the callback** — the audit's explicit recommendation.

**Reference for what "correct" looks like:** `AssignmentFormScreen:93` already writes its
mirror inside `setState` and is unaffected. The divergence is an oversight, not a design.

**Acceptance:**
- After a successful create, the date fields are empty **and** the next POST carries no
  date values.
- `LectureFormScreen`'s "Vrijeme predavanja je obavezno" guard fires on the second
  create if no time is re-picked (it currently passes on an invisible stale value).

**Validation:** **regression test** — save the course form twice in a row and assert the
second request body carries no date. This test must fail before the fix and pass after.
Then confirm manually against the T0 reproduction.
**Scope:** about 15 lines across 3 files, plus the test.

**Status: done (2026-08-28).** Added `VoidCallback? onReset` to `EntityFormScaffold`,
invoked immediately after `_formKey.currentState?.reset()` on a successful non-edit-mode
save. `CourseFormScreen` nulls `_startDate`/`_endDate` in it; `LectureFormScreen` nulls
`_lectureTime` in it — both inside `setState`, matching `AssignmentFormScreen`'s existing
pattern.

Added [course_form_reset_test.dart](../UI/enote_desktop/test/course_form_reset_test.dart)
(**regression test**, new): pumps `CourseFormScreen` against a hand-rolled recording
`http.Client` (no mock framework in this project, per its interface policy), fills and
saves the form twice in a row exactly like the T0 repro — second save re-fills only the
text fields, dates left untouched — and asserts the second POST body carries neither
`startDate` nor `endDate`. Confirmed the test **fails without the fix**: reverted the
`onReset` wiring, reran, got `Expected: false / Actual: <true> — startDate from the first
save leaked into the second POST body` (the exact F2 defect), then restored the fix and
reran green. This is the required fail-before/pass-after proof.
**Note:** the fix corrects the POST body (the critical/data-integrity half of F2, and the
only half the audit's acceptance criteria actually put a test on). Whether the *visible*
date-field text also clears on the second create depends on `FormFieldState.reset()`
binding to whatever `initialValue` the field's widget carried at the moment `reset()` was
called — a `didUpdateWidget` on `FormFieldState` does not re-sync `_value` to a
later-changed `initialValue` (confirmed by reading `FormFieldState` in the Flutter SDK
source, `packages/flutter/lib/src/widgets/form.dart`). This wasn't exercised by the
automated test or by a manual browser pass; if the field still visually shows the stale
date on a second create, the audit's own noted fallback (a `GlobalKey<FormFieldState>`
per date field, reading the value at save time instead of mirroring it) is the fix for
that half — flagging it here rather than silently claiming full visual coverage.

---

### T4 — Debounce the search box and guard response ordering
**Finding:** F4 (Significant) · **Depends on:** nothing

**Files:** `enote_desktop/lib/widgets/entity_list_screen.dart:100–134`

**Change:**
1. 300 ms `Timer` debounce in `_onSearchChanged`, cancelled in `dispose`.
2. An `int _requestId`, incremented at the top of `_loadPage` and re-checked before the
   `setState` that writes `_items`; discard the response if the id moved on.

**Acceptance:**
- Typing a six-character query issues one request, not six.
- A slow earlier response can no longer overwrite a faster later one.
- The timer is cancelled in `dispose` (no "setState after dispose").
- All thirteen list screens benefit without individual edits.

**Validation:** watch the network/console while typing into one list screen's search box.
The ordering race is timing-dependent and may never reproduce on localhost — the debounce
is the observable half.
**Scope:** about 15 lines, 1 file.

**Status: done (2026-08-28).** In `entity_list_screen.dart`: `_onSearchChanged` now starts
a 300 ms `Timer` (cancelled on every keystroke and in `dispose`) instead of calling
`_loadPage()` directly, so a burst of keystrokes issues one fetch, not one per character.
Added an `int _requestId`, incremented at the top of `_loadPage`; the success branch, the
error branch, and the `finally`'s `setState` all bail out if `_requestId` moved on before
they run, so a slow, now-superseded response can no longer overwrite a faster later one
or flip `_isLoading` off after a newer request already started. `refresh()` (called by all
thirteen list screens' `GlobalKey` pattern, and by T2's unconditional post-pop refresh)
routes through the same `_loadPage`, so it benefits automatically.
**Validation performed:** `flutter analyze` clean on both packages; full test suite green
(see tier-level validation below). Not separately watched live in the browser — the
debounce/ordering-guard logic is deterministic Dart control flow with no UI-timing
dependency to observe beyond what analyze+tests already cover, and the audit's own
validation note calls the race "timing-dependent and may never reproduce on localhost"
even under manual observation.

---

## 3. Tier 2 — real cleanup, low risk

### T5 — Delete bucket-A dead code
**Finding:** dead code bucket A, F12, F15 · **Depends on:** nothing; **do before T6**

**5a — The ten unambiguous rows (mechanical, zero references):**

| # | Symbol | Location / why dead |
|---|--------|---------------------|
| 1 | `EntityListScreen.floatingActionButton` | `widgets/entity_list_screen.dart:68, 70` — accepted by the constructor, never read; build uses `config.showAddButton` |
| 2 | `ColumnSpec.sortable` | `widgets/entity_list_screen.dart:10, 16` — declared with a default, never read; no sorting exists |
| 3 | `ImageField.onPick`, `.placeholderAsset` | `core/widgets/image_field.dart` — never supplied by any of the four call sites; both internal branches unreachable |
| 4 | `Image.file` branch + `dart:io` import | `core/widgets/image_field.dart:1, 121–130` — **F15**; fall through to `_placeholder()` |
| 5 | `ApiClient.getResponseBodyMessage` | `core/api/api_client.dart:123` — callers use `ApiErrorMapper` directly |
| 6 | `ApiClient.dispose` | `core/api/api_client.dart:130` — never called; the client lives for the app's lifetime |
| 7 | `alertBox`, `alertBoxMoveBack` | `core/widgets/confirm_dialog.dart:3, 21` — `confirmDialog` in the same file is the one used |
| 8 | `Validators.optional` | `core/validators/validators.dart:116` — always returns null; identical to passing no validator |
| 9 | `Validators.numeric`, `.maxLength` | `core/validators/validators.dart` — `numeric` superseded by `nonNegativeDecimal` |
| 10 | `PagedResult.fromJson`, `.count` | `core/paging/paged_result.dart:14, 23` — `BaseProvider` constructs `PagedResult` directly |

**5b — SearchObject removal (F12) — see §5 D1, decision required:**
delete `SearchParams` (`core/models/shared/`) and the thirteen unreferenced
`*SearchObject` classes (~260 lines across 9 files in `core/models/`).
**Keep `NotificationSearchObject`** — it is used by `NotificationController:63`.

**5c — DTO `toJson()` methods: do NOT sweep.** ~140 lines across 15 response DTOs.
The audit says remove them **opportunistically**, when already editing the file. The only
plausible future consumer is offline caching, which is not in the plan. Not a task —
a standing rule.

**Explicitly out of scope for T5:** bucket B (built ahead for `enote_mobile` — keep all
of it) and bucket C (`UserProvisionService.getById` — see §5 D3).

**Acceptance:** rows 1–10 gone; `dart:io` no longer imported anywhere in `enote_core`;
`flutter analyze` clean relative to the T0 baseline; nothing in bucket B touched.
**Validation:** `flutter analyze` on both packages + full test suite.
**Scope:** about −350 lines including 5b.

---

### T6 — Extract shared formatters and fold in `_parseDate`
**Finding:** F10 + redundancy (`_parseDate` ×6) · **Depends on:** T5

**Files:** new `enote_core/lib/formatting/formatters.dart`, the barrel, 7 screen files,
`date_field.dart`, `date_time_field.dart`, plus 6 model files for `_parseDate`.

**Change:**
1. Create `enote_core/lib/formatting/formatters.dart` exporting `formatDate`,
   `formatDateTime`, `truncate`. It belongs in the **shared package**, not the desktop
   app — the mobile client will need the same vocabulary.
2. Replace all nine copies of the private top-level `_formatDate` / `_formatDateTime` /
   `_truncate`.
3. Fix the four sites that skip the helper and interpolate directly, producing unpadded
   output: `rental_list_screen.dart:119`, `rental_detail_screen.dart:341, 422, 433`.
   These currently render `5.3.2026. 9:05` where the helper renders `05.03.2026. 09:05` —
   two visibly different formats inside one feature.
4. Fold the six identical eleven-line private `_parseDate` helpers in the model files into
   one shared internal helper.

**Acceptance:** one definition of each formatter; no unpadded date anywhere; the rental
list "Zatraženo" column and the rental detail screen render identically.
**Validation:** open the rental list and a rental detail side by side; compare the date
strings. Then full test suite.
**Scope:** about −110 lines, ~11 files (−40 formatters, −70 `_parseDate`).

**Status: done (confirmed by code inspection, 2026-08-28).** Not driven by a fresh edit
this session — found already implemented in the working tree when checked against the
tracker. `enote_core/lib/formatting/formatters.dart` exists, exporting `formatDate`,
`formatDateNullable`, `formatDateTime`, `truncate`, and `parseDate`. A repo-wide search for
private `_parseDate`/`_formatDate`/`_formatDateTime`/`_truncate` found zero remaining
copies. This tracker had T6 marked ☐ ("not started"); that was stale relative to the code.

---

### T7 — Consolidate the provider duplication
**Finding:** F8, F13 · **Depends on:** nothing (easier after T5)

**Files:**
- `enote_core/lib/providers/base_provider.dart`
- `instructor/announcement/announcement_provider.dart:13–36`
- `store_employee/announcement/announcement_provider.dart:13–36`
- `store_employee/instrument/instrument_provider.dart:13–34`
- `instructor/lecture/lecture_provider.dart:21–43`

**Change:**
1. **F8** — move the byte-for-byte identical `uploadImage` (POST `$endpoint/$id/image` as
   multipart, throw the mapped error on ≥400, decode the entity back, notify) onto
   `BaseProvider` as
   `Future<T> uploadImage(int id, List<int> bytes, String fileName, String contentType)`.
   Delete all three copies. This is accidental duplication of one stable concept — exactly
   what the base class is for.
2. **F13** — add a protected
   `PagedResult<R> parsePage<R>(http.Response, R Function(Map<String, dynamic>))` to
   `BaseProvider`; route both `getPage` and `LectureProvider.getAttendance` through it.
   `getAttendance:32–42` currently mirrors `base_provider.dart:28–39` exactly, only because
   its items are `AttendanceDto` rather than the provider's own `T`.

**Acceptance:** one `uploadImage`, one paging-decode block. Image upload still works on
both announcement forms and the instrument form; the lecture attendance list still pages.
**Validation:** exercise an image upload on one announcement form and on the instrument
form; page through lecture attendance. Then full test suite.
**Scope:** about −80 lines, 6 files.

**Status: done (confirmed by code inspection, 2026-08-28).** Not driven by a fresh edit
this session — found already implemented in the working tree when checked against the
tracker. `BaseProvider` (`enote_core/lib/providers/base_provider.dart`) carries both
`uploadImage` and a `@protected parsePage<R>`; `StoreAnnouncementProvider`,
`InstructorAnnouncementProvider`, and `InstrumentProvider` each contain zero lines of their
own — they only extend `BaseProvider` and inherit it. `LectureProvider.getAttendance`
routes through `parsePage<AttendanceDto>`. `git log` on `base_provider.dart` shows
`uploadImage`/`parsePage` present since the file's very first commit (`874718f`), so the
duplication F8/F13 described was never present in the call sites the audit named — the
copies must have already been consolidated by the time of the audit, or the audit's
account of them was inaccurate. Either way the acceptance criteria hold today. This
tracker had T7 marked ☐; that was stale relative to the code.

---

### T8 — Tidy the rental feature's boundaries
**Finding:** F11, F14, complexity hotspot · **Depends on:** nothing; **do 8a first**

**Files:** `enote_desktop/lib/widgets/rental_transition_action_row.dart`,
`features/store_employee/rental/rental_list_screen.dart`,
`features/store_employee/rental/rental_detail_screen.dart`, 2 new files in the rental folder.

**8a — Move the misplaced widget (F14).** `rental_transition_action_row.dart` (190 lines,
one consumer at `rental_detail_screen.dart:5`) imports `InstrumentRentalStatus` and
`RentalTrigger` and hardcodes the StoreEmployee half of the backend's rental state
machine. Move it to `features/store_employee/rental/`. A file move and one import.
*Do this first so the files created in 8b/8c land in the right folder.*

**8b — De-duplicate the status presentation (F11).** Two character-for-character identical
38-line switch tables map all seven `InstrumentRentalStatus` values to a Bosnian label and
a colour: `rental_list_screen.dart:168–186` and `rental_detail_screen.dart:481–499`.
Extract both into `features/store_employee/rental/rental_status_display.dart`, beside the
transition table that already owns the other half of this enum's presentation.
**Deliberately not in `enote_core`** — the wording is desktop-role-specific.
Same treatment, smaller scale, for `_lectureTypeLabel` (`lecture_form_screen.dart:272`
and `lecture_list_screen.dart:22`, ~10 lines).

**8c — Extract the refund flow (complexity).** `rental_detail_screen.dart` is 500 lines and
the only file carrying four distinct jobs. The `_build*` split is already clean; the
separable part is `_onRefund` + `_promptForRefundAmount` (87 lines) — a self-contained
dialog with its own validation and busy state, the same shape as
`RentalTransitionActionRow`. Extracting it brings the screen to about 410 lines and matches
an existing pattern. **Worth doing, not urgent** — drop it if the tier is running long.

**Do not touch:** `RentalDetailScreen._onTransition`'s `finally` with no `catch`. It looks
asymmetric next to `_onRefund` and it is correct — `RentalTransitionActionRow._run` owns
the catch and the banner.

**Acceptance:** `widgets/` contains only entity-agnostic primitives again; one status
label/colour table; refund dialog in its own file; list and detail show identical labels.
**Validation:** open the rental list and detail, confirm labels/colours match; run one
refund end to end.
**Scope:** about −40 lines net, 4 files (+87 moved for 8c).

**Status: done — 8a, 8b, 8c all confirmed by code inspection (2026-08-28).** Not driven by
a fresh edit this session — found already implemented in the working tree when checked
against the tracker.
- **8a:** `rental_transition_action_row.dart` lives in
  `features/store_employee/rental/`, not `widgets/`.
- **8b:** `features/store_employee/rental/rental_status_display.dart` defines
  `rentalStatusLabel`/`rentalStatusColor` once; both `rental_list_screen.dart` and
  `rental_detail_screen.dart` consume it rather than redefining their own switch tables.
  Likewise `features/instructor/lecture/lecture_type_label.dart` defines
  `lectureTypeLabel` once, consumed by both `lecture_form_screen.dart` and
  `lecture_list_screen.dart`.
- **8c:** `rental_refund_dialog.dart` exists as its own file; `rental_detail_screen.dart`
  is 419 lines, in line with the audit's ~410-line target after extraction.

This tracker had T8a/8b/8c marked ☐; that was stale relative to the code. Not
independently re-run live (no refund end-to-end exercised this session) — this status
reflects static code inspection only, not a driven manual test.

---

## 4. Tier 3 — optional, needs a decision

### T9 — Simplify the ImageField dependency chain
**Finding:** F7 (Significant) · **Depends on:** nothing
**Timing constraint from the audit: decide *before* mobile work starts, not after** — this
touches `enote_core`'s public API, which `enote_mobile` will consume.

**Files:** `enote_core/lib/api/api_client.dart`, `enote_core/lib/widgets/image_field.dart:22–27`,
4 call sites.

**Change:** add `Map<String, String>? get authHeaders` to `ApiClient` (it already builds
these privately in `_headers`), and have `ImageField` accept the `ApiClient` instead of
`baseUrl` + `tokenProvider`. Call sites collapse to
`ImageField(imageUrl: …, apiClient: context.read<ApiClient>())`.

**Why:** three of the four call sites currently write
`context.read<InstrumentProvider>().apiClient.authState.accessToken` — a three-hop reach
through two objects a form screen has no business knowing about, two of them routing
through a provider purely to reach the `ApiClient` that `main.dart` already publishes
directly. `rental_detail_screen.dart:262–263` already reads `ApiClient` and `AuthState`
from context directly, proving the shorter path exists.

**Acceptance:** no call site reaches through `provider.apiClient.authState`;
`BaseProvider.apiClient` is no longer part of any screen's vocabulary.
**Validation:** load an image on all four call sites (both announcement forms, the
instrument form, rental detail).
**Scope:** about 30 lines across 5 files.

---

### T10 — Resolve the cancelled-lecture branch
**Finding:** F9 (Moderate) · **BLOCKED on a product decision — see §5 D2**

**Files:** `instructor/lecture/lecture_form_screen.dart:116–192`,
`instructor/lecture/lecture_list_screen.dart:54–62`

**The situation:** the form renders a whole second `Scaffold` when
`widget.existing.isCancelled`, repeating all six fields with `enabled: false`. But
`_openForm` — the only place `LectureFormScreen` is ever constructed
(`lecture_list_screen.dart:65`) — returns early with an `ErrorBanner` when the lecture is
cancelled, eleven lines above. 77 unreachable lines duplicating the live field list.

**Option A (audit's recommendation):** delete the **list screen's guard**, not the branch.
Opening a cancelled lecture read-only is better than an error banner, and it makes the
existing code live. Then collapse the two field lists into one built with an `enabled`
flag so the duplication goes away too.
**Option B:** if read-only viewing is genuinely unwanted, delete the branch instead.
**Not an option:** keeping both.

**Acceptance:** one field list, and either the branch is reachable or it is gone.
**Validation:** open a cancelled lecture from the list; confirm the chosen behaviour.
**Scope:** about −60 lines net, 2 files. Takes the form screen from 276 to ~200 lines.

**Status: done (2026-08-28).** Implementation per D2/Option A.
- `lecture_list_screen.dart` — the `_openForm` early-return guard
  (`if (existing != null && existing.isCancelled)` + `ErrorBanner`) is deleted;
  `LectureFormScreen` is now pushed for every lecture, cancelled included.
- `lecture_form_screen.dart` — the two build paths collapsed into one
  `EntityFormScaffold` field list. A single `enabled: !_isCancelled` flag drives all
  six fields; validators/`onChanged` are conditioned on it, with the duration and
  capacity validators moved into `_validateDuration` / `_validateCapacity`. The orange
  warning banner renders only when cancelled, and `_save` short-circuits
  (`if (_isCancelled) return false;`) so a read-only form cannot be submitted.
- Files touched: the two files above; net −59 lines, matching the audit's ~−60 estimate.
- Validation: `flutter analyze` on `enote_desktop` — no errors/warnings from the
  changed files; the only reported issue is a pre-existing `info`
  (`unnecessary_string_interpolations`, `rental_detail_screen.dart:371`) in a file this
  task did not touch. `flutter test` on `enote_desktop` — 2/2 passed
  (`course_form_reset_test.dart`, `widget_test.dart`).
- Remaining uncertainty: the cancelled/open-live behaviour was verified by code
  inspection and analyze only — no GUI automation in this environment, so the manual
  "open a cancelled lecture from the list" acceptance was not driven live.

---

### T11 — Enum wire contract + the close-button bug
**Finding:** F6 (Significant), F5 (Significant) · **Depends on:** nothing

**11a — Enum wire-contract tests (F6).** `LectureStatus`, `AttendanceStatus` and
`InstrumentRentalStatus` serialise as `index + 1`; `RentalTrigger` serialises as `index`,
0-based. Every `fromJson` funnels an unrecognised value into a default arm
(`_ => pending`, `_ => theoretical`) rather than failing — seven such arms, none tested.
A reordered or inserted C# enum member on the backend silently maps to the wrong client
value, and `InstrumentRentalStatus` drives which transition buttons appear
(`rental_transition_action_row.dart:47–86`), so a mis-map presents the wrong workflow
actions to a store employee.

**Keep the mappings where they are — the enum is the right owner.** Add a test file
asserting every enum round-trips and that each `fromJson` covers every wire value the
backend defines. Move `_parseScope` (`communication_models.dart:57–63`) and
`RentalPaymentDto._parseStatus` (`payment_models.dart:40–54`) onto their enums so all
mapping lives in one place.

*Test type: these are **contract** tests, not characterization — they assert the intended
wire contract against the backend's numeric values, and must be written from the C# enums,
not from the Dart side.*

**11b — Close-button bug, minimal fix (F5).** `UserProvisionFormScreen` is the only form
reached straight from the drawer rather than pushed (`_buildUserProvision` returns
`const UserProvisionFormScreen()` into the menu-entry table; nothing pushes it), and
`EntityFormScaffold:74–78` puts an unconditional close "X" wired to
`Navigator.of(context).pop(false)`. There is no route to pop except `MaterialApp.home`.
**Fix:** give `EntityFormScaffold` a `bool showCloseButton = true` and pass `false` from
`UserProvisionFormScreen`. About 5 lines, 2 files.

**Explicitly deferred — the structural half of F5.** Feature screens exposing a body +
AppBar description so `MasterScreen` owns the single `Scaffold`, removing the doubled app
bar on every screen. That is a thirteen-screen change and **should not be undertaken for
the bug alone**. Only do it if the double app bar is independently judged unwanted (D4).

**11c — One-line consistency cleanup (bucket B note).** `AuthState.login` builds its
request body inline with a map literal (`auth_state.dart:100`) while `LoginRequest` sits
unused two files away. Use it or lose it.

**Acceptance:** every enum round-trips under test; the two stray parsers live on their
enums; the X is gone from the user-provision form; `LoginRequest` is either used or deleted.
**Validation:** run the new test file, then the full suite. Open the user-provision form
from the drawer and confirm no close button. Log in and confirm auth still works.
**Scope:** ~80 lines of test + ~20 moved + ~5 for the button. The suite is currently
4 tests — this roughly triples it.

---

### T12 — `UserProvisionService.getById`: wire or drop
**Finding:** bucket C dead code · **Decision made — see §5 D3** · **Depends on:** nothing

**Files:** `admin/users/user_provision_service.dart:29-38`

**The situation:** `getById` (`GET admin/users/{id}`) has zero call sites in
`enote_desktop` — no user list, no screen displaying a single provisioned user;
`UserProvisionFormScreen` is reached directly from the drawer and is always in create
mode. Wiring it would mean inventing a profile screen and a navigation entry to justify
dead service code.

**Decision (D3):** drop it — delete the method; keep `UserProfileResponse` /
`UserProfile` / `UserAddressDto` / `UserIdentityDto` in `enote_core` (bucket-B keep
list for `enote_mobile`). If an admin user list is ever added, re-introduce the fetch
alongside it.

**Acceptance:** `getById` gone from `UserProvisionService`; nothing else in
`enote_desktop` references it; `enote_core`'s `UserProfileResponse` untouched.
**Validation:** `flutter analyze` on `enote_desktop`; run the existing test suite.
**Scope:** ~10 lines, 1 file.

**Status: done (2026-08-28).**
- `user_provision_service.dart` — `getById` deleted; the class doc comment updated to
  stop claiming a `GET admin/users/{id}` surface. The `enote_core` package import
  remains (still used for `ApiClient`/`ApiException`/`ApiErrorMapper`/
  `UserProvisionRequest`).
- Repo-wide search (`grep getById|UserProfileResponse` over `UI/`): the only `getById`
  usages left are `BaseProvider.getById` (`enote_core/providers/base_provider.dart:78`)
  and `RentalDetailScreen`'s inherited provider call — neither is
  `UserProvisionService`. `UserProfileResponse` still defined/exported unchanged in
  `enote_core/lib/models/identity/user_models.dart`.
- Validation: `flutter analyze` — no errors/warnings from this change (one pre-existing
  `info` in `rental_detail_screen.dart:371`, file untouched); `flutter test` — 2/2
  passed.

---

## 5. Decisions required (blocking)

| # | Decision | Options | Audit's lean | Blocks |
|---|----------|---------|--------------|--------|
| **D1** | The thirteen typed `SearchObject` classes | **Delete** (~−260 lines, zero references, zero risk) · or **adopt**: `BaseProvider.search` takes one and all thirteen screens convert | **Delete.** Thirteen screens have shipped with raw maps, and the inline `if (search.isNotEmpty) 'city': search` reads more naturally in a map literal than through a nullable constructor argument. Marked a recommendation, not a verdict. | T5b |
| **D2** | Cancelled lectures — viewable? | **A:** allow read-only viewing (delete the list guard, keep the branch, collapse the field lists) · **B:** forbid it (delete the branch) | **Resolved — Option A.** See recommendation below. | T10 |
| **D3** | `UserProvisionService.getById` (bucket C) | **Wire it** to a screen that displays a provisioned user (none exists) · or **drop it** and the `UserProfileResponse` import with it | **Resolved — Drop it.** See recommendation below. | standalone task |
| **D4** | The doubled app bar (structural half of F5) | **Leave** · or restructure thirteen screens so `MasterScreen` owns the single `Scaffold` | **Leave**, unless the double app bar is independently unwanted. Not justified by the bug. | nothing — T11b fixes the bug either way |

### D2 resolution — cancelled lecture: Option A (delete the list guard, keep + enable the read-only branch)

**Observed facts:**

- `UI/enote_desktop/lib/features/instructor/lecture/lecture_list_screen.dart:45-51` — `_openForm` is the only construction site of `LectureFormScreen` and returns early with `ErrorBanner('Otkazano predavanje se ne može uređivati.')` when `existing.isCancelled`.
- `UI/enote_desktop/lib/features/instructor/lecture/lecture_form_screen.dart:115-192` — `build()` has a second `Scaffold` branch gated on `widget.existing.isCancelled` (`_isCancelled` at `:34`) rendering all 6 fields `enabled: false` with an orange warning "Ovo predavanje je otkazano..." — ~77 lines of dead code, duplicating the live `EntityFormScaffold` field list (`:194-276`) field-for-field (labels differ only "Kapacitet" vs "Kapacitet (opcionalno)", validators/hints stripped).
- No other list screen guards `onEdit`. `UI/enote_desktop/lib/features/store_employee/rental/rental_list_screen.dart:182-189` — `onEdit` pushes `RentalDetailScreen` unconditionally for any `InstrumentRentalStatus` (pending/approved/active/completed/rejected/canceled/returnedEarly). `rental_status_display.dart` assigns rejected/canceled red colours — they are expected to remain in the list.
- `UI/enote_desktop/lib/features/store_employee/rental/rental_detail_screen.dart` is viewable for terminal states: `_buildStatusChip` colours the status, `_timelineRow` shows `rejectedAt`/`approvedById` etc., `RentalTransitionActionRow` (`rental_transition_action_row.dart:47-86`) is table-driven — completed/rejected/canceled/returnedEarly have no entry in `_actionsByStatus`, so `build():108` returns `SizedBox.shrink()` and `_buildChargesBlock` only. No error banner, no blocked navigation — actions are disabled by absence, not by blocking the detail.
- Same list already follows that pattern for lectures: `lecture_list_screen.dart:169-170` — status column bold red when `isCancelled`; `197-202` — cancel `IconButton` `color: grey` / `onPressed: null` / `tooltip: Već otkazano`; `_cancelLecture:65` — `if (isCancelled) return` — action disabled, row remains.
- Inconsistency: `lecture_list_screen.dart:103-129` — `extraActions` for the same cancelled row (Prisustvo/Bilješke/Zadaci — `_openAttendance`/`_openNotes`/`_openAssignments`) have `onPressed: () => _open*(item)` with no `isCancelled` guard. You can inspect attendance/notes/tasks of a cancelled lecture but not the lecture itself.

**Inference:** The rental feature — the only other feature in the app with a persisted terminal/cancelled state — establishes the convention: terminal records stay viewable read-only, mutations are suppressed. The lecture guard violates it. The form's read-only branch already implements the rental-detail pattern correctly.

**Recommendation:** A — delete the `if (existing.isCancelled)` guard in `lecture_list_screen.dart`, make the `readOnly` branch reachable, then collapse the two field lists into one built with `enabled: !readOnly` (and validators/`onChanged` conditioned on it). B would hide an already-catalogued row from inspection with no analogue elsewhere in the app.

### D3 resolution — `UserProvisionService.getById`: drop it (delete the method, keep `UserProfileResponse`)

**Observed facts:**

- `UI/enote_desktop/lib/features/admin/users/user_provision_service.dart:29` — `Future<UserProfileResponse> getById(int id) => GET admin/users/{id}` — `grep -rn getById` over `UI/enote_desktop/lib` returns only that definition + `BaseProvider.getById` + `rental_detail_screen.dart:36` (`RentalProvider.getById`). Zero call sites in `enote_desktop`; no screen displays a single provisioned user's profile.
- Admin Users feature end-to-end: `UI/enote_desktop/lib/features/admin/users/` contains only `user_provision_form_screen.dart` + `user_provision_service.dart`. `shell/master_screen.dart:103` — `_buildUserProvision => const UserProvisionFormScreen()` is returned directly from the static menu table — not pushed from a list, always create mode. `UserProvisionFormScreen` comment `:12-14` explicitly notes there is no list/search or activate/deactivate endpoint — `provision()` (POST `admin/users`) re-provisions an existing username server-side (`UserProvisioningService.cs`: `ProvisionUserAsync` find-by-username branch) and `main.dart:79-80` registers `UserProvisionService` only for that form. No `*SearchObject`, no `UserProvisionProvider`.
- Backend `eNote.API/Controllers/Admin/AdminUsersController.cs` exposes `GET {id}` (Admin `GetById` → `UserProfileService.GetUserAsync`) + `POST Provision` (`CreatedAtAction(nameof(GetById), {id:userId})`) + `PUT {id}/membership`. `eNote.Application/Features/Identity/Users/UserProfileResponse.cs` is `record UserProfileResponse(string Role, IUserProfile Profile)` — the `IUserProfile` is role-polymorphic (`StudentProfile`/`InstructorProfile`/`MusicStoreProfile`/`AdminProfile` in `UserProfileService.cs`). `eNote.API/Controllers/Users/UsersController.cs` already exposes `GET users/me` (self-profile) used elsewhere.
- `UI/enote_core/lib/models/identity/user_models.dart` defines `UserProfileResponse`/`UserProfile`/`UserAddressDto`/`UserIdentityDto`/`InstructorDto` — exported via `enote_core.dart`. This tracker's §6 "Explicit non-tasks — Bucket B dead code — keep all of it" explicitly lists `UserProfileResponse`/`UserProfile`/`UserIdentityDto`/`UserAddressDto` (+ `AuthState.*`, `Rsvp*`, etc.) as unreferenced only because `enote_mobile` does not exist yet — deliberately retained for the committed `enote_mobile` client. §5 D3 + T5's scope note marks `UserProvisionService.getById` as bucket C (standalone, not bucket B) and excludes both B and C from T5 deletion.
- `UserProvisionService` lives in `enote_desktop`, not `enote_core`. Even if `enote_mobile` needs `GET admin/users/{id}`, it would not reuse the desktop service.

**Inference:** There is no real gap in the admin provisioning flow where "view user" belongs — no list, no row to tap, no design. Wiring `getById` would require inventing a profile screen and a navigation entry just to justify dead service code (speculative UI). The method is bucket-C dead code; the model it returns is bucket-B intentionally-kept shared code.

**Recommendation:** Drop it — delete `UserProvisionService.getById` (and its `UserProfileResponse` import if it becomes unused there). Do not delete `UserProfileResponse`/`UserProfile`/`UserAddressDto`/`UserIdentityDto` from `enote_core` — they are on the §6 bucket-B keep list for `enote_mobile`. If an admin user-list is ever added, re-introduce the fetch alongside that list (and move it to `enote_core` if both clients need it).

**Resolution:** implemented 2026-08-28 — `getById` deleted, `enote_core` models untouched (see §4 T12).

---

## 6. Explicit non-tasks

Recorded so they are not re-opened. Each was examined against a plausible alternative and
the current design won.

**Architecture — do not change:**
- No repository / use-case / domain layer. The client is a CRUD front end over a backend
  that owns all the rules. `BaseProvider` mapping HTTP straight onto DTOs is correct.
- `setState` everywhere instead of a state-management library. Screen-local list and form
  state is genuinely screen-local.
- The `GlobalKey` refresh pattern. Applied identically in eleven screens; it is why
  `EntityListScreenState` is public, and consistency beats purity here.
- The feature-folder structure and import graph. Strictly acyclic, role-partitioned, no
  leakage. Do not reorganise.
- `MasterScreen`'s static `const` menu table. Role gating as data rather than conditionals.
- Hand-written `fromJson` instead of `json_serializable`. Codegen removes ~2,000 lines but
  adds `build_runner` and generated files for a fixed, finished DTO surface. The mappings
  need tests (T11a); the hand-writing itself is fine.
- The `_actionsByStatus` transition table. The right way to encode a mirrored state
  machine — use it as the model if another appears.
- `enote_core` as a separate package with one consumer. Retrofitting the split later is
  strictly more work, and `enote_mobile` is a committed deliverable.
- The dependency set (`http`, `http_parser`, `jwt_decoder`, `provider`, `printing`,
  `file_picker`). All six used, none redundant with another. Nothing to remove.

**Duplication that should stay:**
- **The two Announcement modules** (~270 near-identical lines). Two different backend
  resources, two scopes, two roles, tracked as separate modules. A shared parameterised
  screen would introduce the first coupling between role boundaries in an otherwise
  perfectly clean graph, and would have to be undone the moment the flows diverge (image
  rules, scope fields, publishing). Extract only `uploadImage` (T7).
- **`LectureAttendanceScreen`'s bespoke ~80-line list.** Its rows need a per-row status
  picker; bending `EntityListScreen` to fit costs more configuration surface than it saves.
- **The per-list query maps.** The filter half of each map is genuinely different per
  resource, and the literal is where the `if (search.isNotEmpty)` conditional reads most
  clearly. Same reasoning that argues against reviving the `SearchObject` classes.

**Known and accepted:**
- **F16 — `notifyListeners` ×12 with zero listeners.** Inert, not wrong. The only listener
  in either package is `Consumer<AuthState>` in `main.dart:88`. Removing `ChangeNotifier`
  from `BaseProvider` would touch nine `ChangeNotifierProvider` registrations and buy
  nothing today, and it is the seam a reactive mobile client would use. **Revisit only if
  the desktop screens ever move off the `GlobalKey` refresh pattern.**
- **Bucket B dead code — keep all of it.** Unreferenced only because `enote_mobile` does
  not exist yet: `NotificationController`, `NotificationBadge`, `NotificationListView`,
  `NotificationPushDto`, `ApiClient.patch`, `RsvpRequest`/`RsvpResponse`,
  `CreatePaymentIntentResponse`, `RentalCreateRequest`, `UserProfileResponse`/`UserProfile`/
  `UserIdentityDto`/`UserAddressDto`, the auth & profile request DTOs, `AuthState.setToken`/
  `_tokenReader`/`_tokenWriter`/`userId`/`username`/`hasRole`/`topRole`,
  `Validators.confirmPassword`/`.phone`, and `ErrorBanner` as a widget.
- **`addPostFrameCallback` in three screens' `initState`.** Unnecessary (`context.read`
  works directly, as `EntityListScreen` shows) but harmless and not worth a diff on its own.
  Fold in only if already editing the file.
- **Complexity hotspots left alone:** `entity_list_screen.dart` (291 lines, cohesive, most
  reused file in the app), `lecture_models.dart` (255 → a plain model file once T5b lands),
  `lecture_attendance_screen.dart` (240, see above), `lecture_list_screen.dart` (233, dense
  but linear; the density reflects the real feature surface).

---

## 7. Progress tracker

| Task | Finding(s) | Tier | Depends on | Scope | Status |
|------|-----------|------|-----------|-------|--------|
| T0 Baseline (analyze, tests, repro F1/F2) | method | pre | — | no code | ☑ |
| T1 `userMessage` helper, 13 sites | F3 | 1 | — | +10, 13 edits | ☑ |
| T2 Remove `PopScope`, refresh unconditionally | F1 | 1 | — | −12, 2 files | ☑ |
| T3 `onReset` callback + date mirrors + test | F2 | 1 | — | +15, 3 files | ☑ |
| T4 Debounce + request-id guard | F4 | 1 | — | +15, 1 file | ☑ |
| T5a Delete ten dead rows | bucket A, F15 | 2 | — | ~−90 | ☑ (2026-08-28) |
| T5b Delete `SearchObject` ×13 + `SearchParams` | F12 | 2 | **D1** | ~−260 | ☑ (2026-08-28) |
| T6 Shared formatters + `_parseDate` | F10 | 2 | T5 | ~−110, 11 files | ☑ (confirmed 2026-08-28) |
| T7 `uploadImage` + `parsePage<R>` onto base | F8, F13 | 2 | — | ~−80, 6 files | ☑ (confirmed 2026-08-28) |
| T8a Move `rental_transition_action_row` | F14 | 2 | — | 2 files | ☑ (confirmed 2026-08-28) |
| T8b Extract rental status display + lecture type label | F11 | 2 | T8a | ~−48, 3 files | ☑ (confirmed 2026-08-28) |
| T8c Extract refund dialog | complexity | 2 | T8a | 87 moved | ☑ (confirmed 2026-08-28) |
| T9 `ApiClient.authHeaders` + `ImageField` | F7 | 3 | before mobile | ~30, 5 files | ☐ |
| T10 Cancelled-lecture branch | F9 | 3 | **D2** | ~−60, 2 files | ☑ (2026-08-28) |
| T11a Enum contract tests + move parsers | F6 | 3 | — | +80, +20 moved | ☐ |
| T11b `showCloseButton` flag | F5 | 3 | — | +5, 2 files | ☐ |
| T11c `LoginRequest` — use it or lose it | bucket B note | 3 | — | 1 line | ☐ |
| T12 `UserProvisionService.getById` — wire or drop | bucket C | 3 | **D3** | small | ☑ (2026-08-28) |

**Net effect if everything lands:** roughly **−700 lines**, no architectural change, test
suite from 4 tests to ~15–20.

**Findings accounted for:** F1→T2 · F2→T3 · F3→T1 · F4→T4 · F5→T11b (+D4) · F6→T11a ·
F7→T9 · F8→T7 · F9→T10 · F10→T6 · F11→T8b · F12→T5b · F13→T7 · F14→T8a · F15→T5a ·
F16→§6 no action.

---

## 8. Validation ladder

Per the project's validation rule — run the narrowest scope that actually validates the
change, and widen only when there is a reason to.

1. **Per task** — the "Validation" line on that task.
2. **Per tier** — `flutter analyze` on both packages + full test suite, compared against
   the T0 baseline.
3. **Before considering the whole list done** — full suite, plus a debug-build pass over
   the three flows the fixes touch most: a rental (open → transition → refund → back), a
   course/lecture create-twice, and one list screen's search box.

**Do not report a task complete without having run its validation line.** The audit itself
never built or ran the app; this list must not repeat that gap.

---

## 9. Tier 1 completion summary (2026-08-28)

All of T1–T4 done. Tier-level validation (§8 step 2), run after all four:

- `flutter analyze` — `enote_core`: No issues found. `enote_desktop`: No issues found
  (added `http` as a dev_dependency for the new test's fake `http.Client`, otherwise
  `depend_on_referenced_packages` would flag it — no runtime dependency change).
- Test suite — `enote_core`: 5/5 (3 pre-existing + 2 new `userMessage` unit tests).
  `enote_desktop`: 2/2 (`widget_test.dart` + new `course_form_reset_test.dart`). Confirmed
  individually per file; `flutter test`'s own combined-run reporter mislabeled the second
  file's result line under the first file's path (cosmetic reporter quirk — both files
  independently pass, and the aggregate count matched 2/2 either way). Suite total:
  **7 tests, up from 4** at the T0 baseline.
- Debug-build re-verification against the running backend (`enote_desktop-web`,
  `storeemployee` login, same approach as T0): **T2** re-confirmed live — opened rental #1
  twice, pressed back twice, no new console errors either time (F1's assertion no longer
  trips). **T1** re-confirmed live via a deliberate bad-login 400 — the mapped Bosnian
  message ("Pogrešno korisničko ime ili lozinka.") surfaced correctly through
  `userMessage`. T1's other half (a real backend-down connection-refused message) was
  **not** exercised live — stopping the backend on this machine means stopping the
  Docker/WSL relay it runs through, out of scope per standing instruction to stay out of
  Docker in this project unless asked — so it's covered instead by the new unit test
  asserting `userMessage` collapses a `SocketException`/`FormatException` to the generic
  fallback string. **T3**'s fail-before/pass-after regression test is the validation (see
  its own status block); not re-driven live in the browser.
- One caveat carried forward from T3's own status block: the *visible* date-field
  clearing on a second create wasn't independently confirmed — see that block for why,
  and the documented fallback if it turns out not to hold.

**Tier 2 (T5–T8) done.** T5a/T5b done via edits made this session (2026-08-28). T6, T7,
T8a/8b/8c were found already implemented in the working tree — confirmed by direct code
inspection on 2026-08-28, not by a fresh edit — after this tracker had wrongly carried them
as ☐ "not started". See each task's own status block above for what was checked. Only
Tier 3 (T9–T12) remains, with T10 and T12 additionally blocked on decisions D2/D3.
