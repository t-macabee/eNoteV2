# Gap mitigation task list — eNoteV2 mobile client

**Scope of this file.** One task per gap listed in `01-blueprint.md` §11. Nothing else. No scaffolding tasks, no feature-screen tasks, no build plan.

**Count.** 15 gaps: brief items 1–7 (§11.1) and A1–A8 (§11.2). Every one has exactly one task below.

**Task fields.** Each task states: the gap it closes, where the gap bites, the exact change, the files, the done-when condition.

**Status vocabulary.** `not done` · `done` · `not needed`. No other value.

**All decisions are taken.** Nothing below is blocked on input. See §0.

---

## 0. Decisions taken (2026-09-09)

| ID | Question | Decision | Consequence |
|---|---|---|---|
| **Q-A** | Currency: KM label vs Stripe charge currency | **Set `STRIPE_CURRENCY=bam`. KM everywhere.** | One line in `.env`. Every screen including the PaymentSheet reads KM. Desktop parity kept. No split-currency screen. |
| **Q-B** | Do students get an events view? | **Yes.** | M4 is required. Adds a "Događaji" sub-tab under Učenje. |
| **Q-C** | Do students get music-store browsing? | **No.** | M5 is `not needed`. The store name stays plain text on the instrument card. |
| **Q-D** | Do announcements need search? | **Yes.** | M6 is required. `Title` on the search object, server-side. |
| **Q-E** | Debt check: endpoint or client approximation? | **Server endpoint.** | M7 builds `GET student/rentals/debt`. The client approximation is dropped. |
| **Q-F** | Is `membershipPaidUntil` valid through that day? | **Yes — inclusive.** | M11 compares against the end of that day, not against the raw timestamp. |

---

## 1. Backend gaps — `eNote.API` / `eNote.Application` / `eNote.Infrastructure`

### M1 — `CourseDto` carries no per-student enrolment flag
- **Closes:** A1
- **Where it bites:** course detail cannot choose between "Upiši se" and "Ispiši se". Without this the screen shows both actions disabled and the text *"Status upisa nije dostupan."*
- **Change:** add `bool IsEnrolled` to `CourseDto`. Populate it only in `GetPagedForStudentAsync` and `GetByIdForStudentAsync` as `Enrollments.Any(e => e.StudentId == studentId && e.EnrollmentStatus == Active)`. Leave it `false` on every other route that returns `CourseDto`. Add `bool? EnrolledOnly` to `CourseSearchObject`, applied on the student route only.
- **Files:** `CourseDto.cs`, `CourseService.cs`, `CourseSearchObject.cs`
- **Done when:** `GET student/courses` and `GET student/courses/{id}` return `isEnrolled: true` for a course the seeded `student` is enrolled in, `false` otherwise. Admin and instructor course routes return the same payloads as before.
- **Status:** not done

### M2 — `LectureDto` carries no attendance status for the current student
- **Closes:** A2
- **Where it bites:** RSVP buttons cannot show the answer the student already gave. The answer survives only until the screen is rebuilt.
- **Change:** add `AttendanceStatus? MyAttendanceStatus` to `LectureDto`. Populate from the current student's own row in `Attendances` inside the student-facing methods of `LectureService`. Leave it `null` on every other route.
- **Files:** `LectureDto.cs`, `LectureService.cs`
- **Done when:** `GET student/lectures` and `GET student/lectures/{id}` return the caller's own status, or `null` when they have not answered. Instructor and admin lecture routes are unchanged.
- **Status:** not done

### M3 — no student endpoint for own submission or grade
- **Closes:** A3
- **Where it bites:** the assignment screen cannot show a graded or ungraded state. Today the grade appears only inside a notification body and inside the ranking average.
- **Change:** add `GET api/v1/student/assignments/{id}/submission` returning `AssignmentSubmissionDto`, and 404 through the existing `AppException` not-found path when no submission exists. Add `GET api/v1/student/submissions` with `page`, `pageSize`, `includeTotalCount` for submission history. Both `[Authorize(Roles = Student)]`. Derive ownership from the `sub` claim only — never from a route or body parameter.
- **Files:** `AssignmentSubmissionController.cs`, `AssignmentSubmissionService.cs`, `AssignmentSubmissionSearchObject.cs` (new)
- **Done when:** submit an assignment, then the detail endpoint returns that submission with its grade. An assignment with no submission returns 404 in the standard `{status, code, message}` shape. The history endpoint pages.
- **Status:** not done

### M4 — no student-facing events endpoint
- **Closes:** brief item 2 · **Decision Q-B: build it**
- **Where it bites:** students have no events view. Announcements are the only feed.
- **Change:** add `GET api/v1/student/events` returning `PagedResult<EventDto>`, query `page`, `pageSize`, `includeTotalCount`, `from`, `to`, `[Authorize(Roles = Student)]`. Filter to events with no course association plus events on courses the caller is actively enrolled in. Before reusing `EventDto`, check it for fields a student must not see; if any exist, add a trimmed student DTO instead.
- **Files:** new `StudentEventController.cs`, `EventService.cs`, `EventSearchObject.cs`
- **Done when:** the seeded `student` receives platform-wide events plus their enrolled-course events, correctly paged, and nothing else.
- **Client side:** a `Događaji` sub-tab inside the Učenje tab bar, alongside Kursevi · Predavanja · Zadaci · Objave. Not a fifth bottom-nav destination.
- **Status:** not done

### M5 — no public music-store browse endpoint
- **Closes:** brief item 3 · **Decision Q-C: not building it**
- **On record:** `shop/store` is StoreEmployee-only. `InstrumentDto.musicStore` carries the store name and nothing else. A student sees the store name as plain text and cannot open it. No course rule requires a store page, and the rental flow arranges pickup through the rental itself, so store contact details are never needed by a student.
- **Change:** none. The store name renders as unclickable text on the instrument card and detail screen.
- **If reversed later:** `GET api/v1/stores/public` (paged `MusicStoreDto` with image path) and `GET api/v1/stores/public/{id}`, both `[AllowAnonymous]`, matching the `instruments/public` pattern, plus a store detail screen.
- **Status:** not needed

### M6 — `student/announcements` has no search parameter
- **Closes:** A5 · **Decision Q-D: add it**
- **Where it bites:** the announcement list can be paged but not searched, while every other list in the app has search.
- **Change:** add `string? Title` to `AnnouncementSearchObject` and apply it in `StudentAnnouncementFeedService`. Server-side only — no client-side filtering over a single page.
- **Files:** `AnnouncementSearchObject.cs`, `StudentAnnouncementFeedService.cs`
- **Done when:** `GET student/announcements?title=x` filters across all pages, and the mobile list ships with a search field like every other list.
- **Status:** not done

### M7 — no debt-status endpoint
- **Closes:** A6 · **Decision Q-E: server endpoint**
- **Where it bites:** `POST student/rentals` rejects a request when a completed or returned rental is unpaid, but the client cannot tell the student **before** they fill the form, and cannot link them to the rental they owe on.
- **Change:** add `GET api/v1/student/rentals/debt` returning `{ hasUnpaidDebt: bool, rentalId?: int }`, `[Authorize(Roles = Student)]`, resolved from the caller's `sub` claim. `rentalId` is the first unpaid completed or returned rental, so the client banner can deep-link to "Plati".
- **Files:** `StudentRentalController.cs`, `RentalQueryService.cs`
- **Done when:** requesting a rental while a completed rental is unpaid shows a blocking banner with a working link to that rental's payment screen, before the form is submitted. The server message is still shown verbatim if the server rejects anyway.
- **Status:** not done

### M8 — currency mismatch between the app and Stripe
- **Closes:** A7 · **Decision Q-A: `STRIPE_CURRENCY=bam`, KM everywhere**
- **Where it bites:** core `formatKM` renders every fee as `KM`. Stripe charges in `STRIPE_CURRENCY`, `eur` by default, and `RentalPaymentDto.currency` reports it. Left alone, the pay screen would show "120.00 KM" above a PaymentSheet saying "€120.00".
- **Change:** set `STRIPE_CURRENCY=bam` in `.env` and `docker-compose.yml`. Every screen, including the PaymentSheet, then reads KM. Mobile uses `formatKM` throughout, matching desktop. No screen-specific currency logic.
- **Files:** `.env`, `docker-compose.yml`
- **Done when:** a test-mode PaymentIntent is created in `bam` and the PaymentSheet displays the same amount and currency as the rental screen.
- **Verify first:** confirm the Stripe test account accepts `bam` as a presentment currency for a PaymentIntent. If Stripe rejects it, stop and report — do not silently fall back. The alternative is KM on rental screens with the real currency on the pay screen only.
- **Optional, not decided:** `GET api/v1/payments/config` returning `{ publishableKey }` would remove the compile-time `--dart-define`. Not required by any gap.
- **Status:** not done

---

## 2. Shared core gap — `UI/enote_core/lib/`

### M9 — validation errors are unparsed and collapse to a generic message
- **Closes:** A4
- **Where it bites:** FluentValidation runs without an `InvalidModelStateResponseFactory` override, so request-validation failures return ASP.NET `ValidationProblemDetails` — `{type, title, status, errors: {field: [msg]}}`. `ApiErrorMapper` reads only `message`, so every field error becomes *"Neispravan zahtjev."* This affects **both** clients today, not only mobile.
- **Change:** in `ApiErrorMapper.mapError`, after the existing `message` check: if the body has an `errors` object, join every message in every field array with `\n` and return it; else if `status == 400` and `title` is non-empty, return `title`. Leave all other branches alone. Add optional `Map<String, List<String>>? errors` to `ApiError.fromJson`.
- **Files:** `UI/enote_core/lib/api/api_error_mapper.dart`
- **Done when:** `flutter test` passes in `UI/enote_core` and in `UI/enote_desktop`. Specifically: the empty-400-body case in `api_response_test.dart` still returns the old default, and the two `{'message': ...}` cases in `delegated_user_screens_test.dart` still pass unchanged.
- **Status:** not done

**M9b — typo in the same file.** `api_error_mapper.dart:66` reads `'Vaša sesija je istečla. Prijavite se ponovo.'`. Correct spelling is `istekla`. **Verified:** the string occurs in exactly one place in the repository and no test in `enote_core` or `enote_desktop` pins it. Safe to change with M9. Not one of the 15 gaps; noted here because it is the same file and the same edit.

---

## 3. Client-side gaps — closed in `UI/enote_mobile`, no backend change

### M10 — rental timeline must be assembled client-side, and actor fields are raw IDs
- **Closes:** brief item 4
- **Where it bites:** `InstrumentRentalDto` exposes `approvedById` and `rejectedById` as integers and carries no actor names. Rendering them would put a database id in front of a student.
- **Change:** build the timeline from the six timestamps only — `requestedAt`, `approvedAt`, `rejectedAt`, `pickedUpAt`, `returnedAt`, `paidAt`. Render every store-side step as the store, using `storeName` from the DTO. Never render a person and never render an id. All amounts use core `formatKM` (Q-A).
- **Files:** `features/rentals/rental_timeline.dart`
- **Done when:** every rental state renders a labelled step, and no screen displays `approvedById` or `rejectedById` in any form.
- **Status:** not done

### M11 — membership is visible to the student but not renewable by them
- **Closes:** brief item 5 · **Decision Q-F: inclusive boundary**
- **Where it bites:** `users/me` returns `membershipPaidUntil`. Renewal exists only on admin routes. An expired membership must block enrolment and rental requests, and the student must be told why.
- **Change:** `SessionController.isMembershipActive` is true when `membershipPaidUntil` is set and **the end of that calendar day** has not passed. A student whose membership reads `09.09.2026.` can still act on 09.09. When inactive, render enrolment and rental actions disabled with a `BlockedReasonBanner`, not hidden. Copy: *"Članarina je istekla {datum}. Obratite se školi za obnovu."* when a date exists, *"Članarina nije aktivna."* when it does not. Always show the server's message verbatim if the server rejects an action the client thought was allowed.
- **Files:** `session/session_controller.dart`, `widgets/blocked_reason_banner.dart`, `features/profile/membership_card.dart`
- **Done when:** a membership dated today still permits enrolment and rental requests. A membership dated yesterday disables both with a visible reason. No action silently disappears from the UI.
- **Note:** if the server later proves to use a stricter boundary, the server wins and its message is displayed. The client never claims an action is allowed after the server refuses it.
- **Status:** not done

### M12 — non-Student tokens reach a client built only for students
- **Closes:** brief item 7
- **Where it bites:** `AuthState` is shared and decodes four roles plus `is_manager`. An admin, instructor, or store employee can log in with valid credentials and would otherwise land in an empty or broken shell.
- **Change:** `SessionGate` renders `RoleBlockedScreen` when the token is valid but `!hasRole('Student')`. Copy: *"Ova aplikacija je namijenjena studentima. Prijavite se na desktop aplikaciju."* plus an "Odjavi se" button. `is_manager` is ignored on mobile.
- **Files:** `shell/session_gate.dart`, `shell/role_blocked_screen.dart`
- **Done when:** logging in as the seeded `admin`, `instructor`, and `storeemployee` each lands on the explained dead end with a working logout, and never on an empty shell.
- **Status:** not done

### M13 — no "payment succeeded" push exists
- **Closes:** A8
- **Where it bites:** finalisation is webhook-only. Three SignalR pushes exist — rental status changed, lecture cancelled, submission graded — and a refund dispatch. None fires on payment success, so the client has no event to wait for.
- **Change:** after `presentPaymentSheet()` returns without throwing, poll `PaymentProvider.status(rentalId)` up to 5 times at 2-second intervals until `succeeded`, then refetch the rental to flip `isPaid`. If the status is still `requiresAction` when the window closes, show *"Plaćanje se obrađuje. Provjerite ponovo za nekoliko sekundi."* with a retry button. Never show "Plaćeno" on client-side optimism.
- **Files:** `features/payments/payment_provider.dart`, `features/payments/payment_status_view.dart`
- **Done when:** a completed test payment flips to paid within the polling window, and an incomplete one shows the processing state instead of a false success.
- **Verified by:** M14
- **Status:** not done

---

## 4. Configuration and documentation gaps

### M14 — Stripe keys are placeholders
- **Closes:** brief item 6
- **Where it bites:** `.env.docker.example` carries `sk_test_...` and `whsec_...` placeholders. No payment can complete. The real `.env` was not read, so its current contents are unknown.
- **Change:** put real test-mode `sk_test` and `whsec` on the server. Set `STRIPE_CURRENCY=bam` in the same pass (M8). Pass `pk_test` to the app as `--dart-define=STRIPE_PUBLISHABLE_KEY=...`. Run `stripe listen --forward-to localhost:5059/api/v1/payments/stripe/webhook` during testing. Set `SMTP_PASSWORD_RESET_URL=enote://reset-password` so the reset mail links into the app.
- **Files:** `.env`, `docker-compose.yml`, run configuration
- **Done when:** a test card completes end to end **and** the webhook finalises the payment server-side. Client-side success alone does not close this task.
- **Status:** not done

### M15 — README is wrong
- **Closes:** brief item 1
- **Where it bites:** `README.md:44` states the Flutter client is in a separate repository. It is not — `UI/` is in this repository. Seeded credentials are also stale or missing.
- **Change:** remove the separate-repository claim. Document `UI/enote_core`, `UI/enote_desktop`, and `UI/enote_mobile`. State the real base URL default `http://10.0.2.2:5059/api/v1/` — note the course text's `http://10.0.2.2:5000` omits the `/api/v1/` suffix and will not work. List the seeded users `admin`, `instructor`, `student`, `storeemployee` with password `Test1234!` unless `Seed__DefaultPassword` is overridden.
- **Files:** `README.md`
- **Done when:** a reader can clone, run, and log in using only the README.
- **Status:** not done

---

## 5. Coverage map

| Gap | Source | Task | Decision | Status |
|---|---|---|---|---|
| 1 — README stale | §11.1 | M15 | — | not done |
| 2 — no student events endpoint | §11.1 | M4 | Q-B: build | not done |
| 3 — no student store browse | §11.1 | M5 | Q-C: skip | **not needed** |
| 4 — timeline client-side, raw actor IDs | §11.1 | M10 | — | not done |
| 5 — membership not renewable by student | §11.1 | M11 | Q-F: inclusive | not done |
| 6 — Stripe keys are placeholders | §11.1 | M14 | — | not done |
| 7 — non-Student token in a student app | §11.1 | M12 | — | not done |
| A1 — no `IsEnrolled` on `CourseDto` | §11.2 | M1 | — | not done |
| A2 — no `MyAttendanceStatus` on `LectureDto` | §11.2 | M2 | — | not done |
| A3 — no own-submission endpoint | §11.2 | M3 | — | not done |
| A4 — `ValidationProblemDetails` unparsed | §11.2 | M9 | — | not done |
| A5 — no announcement search | §11.2 | M6 | Q-D: add | not done |
| A6 — no debt endpoint | §11.2 | M7 | Q-E: endpoint | not done |
| A7 — KM vs EUR | §11.2 | M8 | Q-A: BAM | not done |
| A8 — no payment-succeeded push | §11.2 | M13 | — | not done |

**14 tasks to execute. 1 closed as `not needed`. Nothing blocked.**

Backend work, one pass: M1, M2, M3, M4, M6, M7.
Core work, one file: M9 + M9b.
Client work: M10, M11, M12, M13.
Config and docs: M8, M14, M15.

---

## 6. Open questions that are not gaps

Listed so they are not lost. They belong to the build plan, not to this list.

1. `AuthState` has no method to adopt the token returned by `auth/register`. Either call `login` again after register — one extra round-trip — or add `applyToken(String)` to core.
2. `signalr_netcore` was last published about 12 months ago. Its build health on Flutter 3.47 and its query-string token behaviour are unverified. Polling already satisfies the auto-refresh rule if it fails.
3. No server-side maximum `pageSize` was found in `BaseSearchObject`. The client caps itself at 50.
