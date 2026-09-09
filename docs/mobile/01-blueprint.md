# 01 — Architectural blueprint: eNoteV2 mobile client (`UI/enote_mobile`)

Deliverable 1 of 4 (brief §7.1). Companion files: `02-design.md`, `03-plan.md`, `04-tasks.md`.

**Verified against:** working tree at `main` / `c98f9a3` on 2026-09-09 (the brief cites `507dbb8`; three later commits touched only desktop tests and shared test helpers, nothing below changed). Flutter on this machine: `3.47.1 stable`, Dart SDK constraint in `enote_core` is `^3.13.0`.

> **Update — 2026-09-09, backend gap closure landed.** The six backend mitigations from `gap-mitigation.md` (M1, M2, M3, M4, M6, M7) and the currency change (M8) are implemented in the working tree. Build succeeds; backend suite passes 400/400. Rows G10, G11 and G12 below, §6.1, §6.4, §11 and §13 are revised to match. Anything still marked `not done` in `gap-mitigation.md` awaits live verification against a seeded database, not code.

**Labels.** Every non-trivial statement carries one of: `Observed fact` (read from the repository or pub.dev today), `Inference` (follows from observed facts), `Recommendation` (a decision this blueprint makes), `Open question` (could not be verified; never asserted).

**Language.** User-facing copy is Bosnian (settled by the addendum). Identifiers, file names and this document are English.

---

## 1. Ground truth that drives the design

All rows are `Observed fact` unless marked.

| # | Fact | Where |
|---|---|---|
| G1 | `enote_core` exports models, paging, `ReadOnlyProvider`/`CrudProvider`/`BaseProvider`, `Validators`, `ApiClient`, `ApiErrorMapper`, `ApiException`, `decodeOrThrow`/`throwIfError`, `AuthState`, `NotificationController`, and widgets `confirmDialog`, `ErrorBanner`, `ImageField`, `ImageThumbnail`, `networkImageOrPlaceholder`, `UserAvatar`/`userPictureUrl`, `NotificationBadge`, `NotificationListView`, `InfoRow`, `EntityToolbar`, `PagedFetchController`/`PagedPaginationBar`, formatters. | `UI/enote_core/lib/enote_core.dart` |
| G2 | `AuthState` takes **synchronous** hooks: `String? Function()? tokenReader` and `void Function(String?)? tokenWriter`; it reads the token in its constructor and decodes `sub`, `unique_name`, `role`, `is_manager`. `isAuthenticated` is `token != null && !expired`. `logout()` clears local state only and does **not** call the server. It also accepts an injectable `http.Client`. | `auth/auth_state.dart` |
| G3 | `ApiClient` builds `'$baseUrl$path'`, attaches `Authorization: Bearer`, exposes `authHeaders`, has GET/POST/PUT/PATCH/DELETE and multipart PUT/POST with a `file` part. No interceptor, no timeout, no 401 hook. Accepts an injectable `http.Client`. | `api/api_client.dart` |
| G4 | `ApiErrorMapper.mapError` reads only `message` from the body; otherwise a per-status Bosnian default (`401 → 'Vaša sesija je istečla. Prijavite se ponovo.'`, `400 → 'Neispravan zahtjev.'`). `userMessage()` collapses non-`ApiException` errors to `'Nije moguće povezati se sa serverom. Pokušajte ponovo.'`. | `api/api_error_mapper.dart` |
| G5 | Backend error middleware emits `{status, code, message}` for `AppException` (business, not-found, auth). FluentValidation runs through `AddFluentValidationAutoValidation()` with **no** `InvalidModelStateResponseFactory` override, so request-validation failures return ASP.NET's default `ValidationProblemDetails` (`{type, title, status, errors: {field: [msg]}}`) — a shape G4 does not parse. | `eNote.API/Extensions/MiddlewareExtensions.cs`, `ValidationExtensions.cs` |
| G6 | `NotificationController` polls `GET {endpoint}/unread-count` every 30 s, `refresh()` loads one page of 50 (`pageSize: 50`, no `page` param), `markRead`/`markAllRead` PATCH. `NotificationPushDto` model exists (rentalId/lectureId/submissionId, title, body, createdAt — **no id, no isRead**). | `notifications/notification_controller.dart`, `models/communication/communication_models.dart` |
| G7 | SignalR hub `/hubs/notifications`, `[Authorize]`, group `user:{sub}`, client method `ReceiveNotification`, payload = `NotificationPushDto`. JWT bearer accepts `?access_token=` **only** for paths under `/hubs`. Three push consumers: rental status changed, lecture cancelled, submission graded. Refund dispatches a notification too (`DispatchPaymentRefundedAsync`); there is **no** "payment succeeded" push. | `Hubs/NotificationHub.cs`, `Extensions/IdentityExtensions.cs`, `Consumers/*`, `RentalPaymentService.cs` |
| G8 | Every validated token is checked against `RevokedToken` (`OnTokenValidated → ITokenRevocationService.IsRevokedAsync`), so `POST auth/logout` really invalidates the token; a revoked token yields 401 on the next call. `Jwt__ExpirationDays: 7` in `docker-compose.yml`. No refresh-token endpoint exists. | `IdentityExtensions.cs`, `AuthController.cs`, `docker-compose.yml` |
| G9 | Password reset email link = `{Smtp:PasswordResetUrl}?email=<url-encoded>&token=<url-encoded>` (uses `&` if the base already has `?`). Subject `Reset lozinke`. | `eNote.Infrastructure/Identity/SmtpEmailService.cs` |
| G10 | `POST student/rentals/{id}/payments/create-intent` reuses a `RequiresAction` intent created within 30 min; fails with business errors unless status is Completed/ReturnedEarly, picked up, and unpaid. Finalisation is webhook-only. `POST student/rentals` throws `Messages.RentalUnpaidDebt` (`'Imate neizmireno dugovanje od prethodnog iznajmljivanja. Izmirite ga prije novog zahtjeva.'`) when a completed/returned rental is unpaid. There is **no** endpoint that exposes the publishable key. **Revised 2026-09-09:** `GET student/rentals/debt` now exists (`RentalDebtDto { hasUnpaidDebt, rentalId? }`, `RentalQueryService.GetDebtForStudentAsync`), scoped by `StudentProfile.AppUserId == currentUser.UserId` with the same `(Completed \| ReturnedEarly) && !IsPaid` predicate as the create guard, earliest `RequestedAt` first. Stripe currency is now `bam` in all eight places, so every screen and the PaymentSheet read KM. | `RentalPaymentService.cs`, `RentalCommandService.cs:35-44`, `Messages.cs:69`, `RentalQueryService.cs`, `StripeOptions.cs` |
| G11 | `BaseSearchObject { page=1, pageSize=20, includeTotalCount=true }`. Search fields: courses `name`; lectures `courseId, name, lectureType, from, to`; assignments `title, dueAfter, dueBefore`; lecture notes `title`; instruments `search, model, manufacturer, instrumentTypeId, musicStoreId, isAvailable`; rentals `instrumentId, rentalStatus`; notifications `isRead`. **Revised 2026-09-09:** announcements now take `title` (server-side `Contains` in `StudentAnnouncementFeedService`); courses now also take `enrolledOnly` (bool?, student route only); `student/submissions` takes page fields only; `student/events` takes `from, to` through the existing `ApplySearch`. Max page size is not enforced server-side (`Open question`: no clamp found in `BaseSearchObject`; client must stay ≤ 50). | `eNote.Application/**/*SearchObject.cs` |
| G12 | Student read paths are scoped by enrolment: `student/lectures` and `student/assignments` use `ForEnrolledStudent(studentId)`; `student/courses` lists **published** courses; `student/courses/{id}` returns published-or-enrolled. **Revised 2026-09-09 — all three former holes are closed.** `CourseDto.IsEnrolled` is populated in `GetPagedForStudentAsync`/`GetByIdForStudentAsync` only (`Enrollments.Any(Active)`; `false` on admin/instructor routes). `LectureDto.MyAttendanceStatus` is populated from the caller's own `Attendances` row on the two student paths only, both of which `.Include(x => x.Attendances)`; `null` elsewhere. `GET student/assignments/{id}/submission` (404 via `NotFoundException`) and `GET student/submissions` (paged, `SubmittedAt` desc) both derive ownership from the `sub` claim. | `CourseService.cs:100-152`, `LectureService.cs:22-70`, `AssignmentSubmissionService.cs:21-64`, `AssignmentSubmissionController.cs` |
| G13 | Uploaded file paths are origin-relative and already carry the API prefix: `"/api/v1/uploads/{subfolder}/{file}"`. `networkImageOrPlaceholder` resolves a leading-`/` URL against the client origin and attaches bearer headers. Instrument and store images are anonymous; assignment files are `[Authorize]`. | `LocalFileStorageService.cs:11,130`, `widgets/network_image.dart`, `UploadsController.cs` |
| G14 | `RegisterRequest` = `{username, email, password, firstName?, lastName?}`; validator: username not empty, email format, password ≥ 8. Register provisions a **Student** (`RegisterStudentAsync`) and returns `AuthResponse` with a token. | `RegisterRequest.cs`, `RegisterRequestValidator.cs`, `Infrastructure/Identity/AuthService.cs:65-84` |
| G15 | Desktop wiring: `main()` builds `AuthState(baseUrl:)` + `ApiClient` once, `MultiProvider` with `.value` for those two and `ChangeNotifierProvider(create:)` (lazy) for every feature provider, `Consumer<AuthState>` chooses `LoginScreen` vs `MasterScreen`. `config.dart` = `String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:5059/api/v1/')`. Theme tokens live in `enote_desktop/lib/theme/app_theme.dart` (not in core). | `enote_desktop/lib/main.dart`, `config.dart`, `theme/app_theme.dart` |
| G16 | Dev seed users: `admin`, `instructor`, `student`, `storeemployee` (password `Seed__DefaultPassword`, default `Test1234!`). README says the Flutter client is "in a separate repository" — false. | `Infrastructure/Data/Seed/IdentitySeed.cs:32-35`, `README.md:44` |
| G17 | Pinned versions in `enote_desktop/pubspec.lock`: `provider 6.1.5+1`, `http 1.6.0`, `http_parser 4.1.2`, `jwt_decoder 2.0.1`, `file_picker 10.3.10`, `flutter_lints 6.0.0`, `lints 6.1.0`; SDK `dart >=3.13.1`, `flutter >=3.41.0`. | `UI/enote_desktop/pubspec.lock` |
| G18 | pub.dev today: `flutter_stripe 14.0.0` (minSdk 21, needs `FlutterFragmentActivity` + `Theme.AppCompat` descendant, PaymentSheet supported); `flutter_secure_storage 11.0.0` (minSdk 23); `app_links 7.2.1` (`uriLinkStream` replays the initial link); `signalr_netcore 1.4.4` (`accessTokenFactory`, `withAutomaticReconnect()`, last published ~12 months ago). | pub.dev, fetched 2026-09-09 |

---

## 2. Package and folder structure

`Recommendation`. Third package beside the two existing ones, path-dependent on `enote_core`, mirroring the desktop's feature-per-folder and `*_screen` / `*_provider` naming.

```
UI/enote_mobile/
├── pubspec.yaml                      # §10
├── analysis_options.yaml             # same flutter_lints ^6.0.0 as desktop
├── android/                          # flutter create --platforms=android; minSdk 23; FlutterFragmentActivity; intent-filters (§5.4)
├── lib/
│   ├── main.dart                     # bootstrap: read token, build AuthState/ApiClient, runApp
│   ├── config.dart                   # kApiBaseUrl, kStripePublishableKey (both String.fromEnvironment)
│   ├── app.dart                      # MaterialApp(navigatorKey, theme, onGenerateRoute, home: SessionGate)
│   ├── theme/
│   │   └── app_theme.dart            # same tokens as desktop (§3.3 explains why a copy)
│   ├── shell/
│   │   ├── session_gate.dart         # Consumer<AuthState>: Login | RoleBlocked | RootShell
│   │   ├── root_shell.dart           # Scaffold + NavigationBar (4 destinations) + AppBar bell
│   │   ├── role_blocked_screen.dart  # non-Student token → explained dead end + "Odjavi se"
│   │   └── app_router.dart           # named route table + typed argument classes
│   ├── session/
│   │   ├── token_store.dart          # flutter_secure_storage wrapper with a sync in-memory mirror
│   │   ├── session_http_client.dart  # http.BaseClient: timeout + 401 → onUnauthorized
│   │   ├── session_controller.dart   # ChangeNotifier: profile cache, membership state, logout+revoke
│   │   └── deep_link_handler.dart    # app_links → reset-password / stripe-redirect routing
│   ├── realtime/
│   │   └── notification_hub_client.dart  # signalr_netcore connection bound to NotificationController
│   ├── features/
│   │   ├── auth/         login_screen, register_screen, forgot_password_screen, reset_password_screen
│   │   ├── courses/      course_list_screen, course_detail_screen (master–details), course_provider
│   │   ├── lectures/     lecture_list_screen, lecture_detail_screen, rsvp_sheet, lecture_provider
│   │   ├── lecture_notes/ lecture_note_list_screen, lecture_note_detail_screen, lecture_note_provider (screen-scoped)
│   │   ├── assignments/  assignment_list_screen, assignment_detail_screen, assignment_provider
│   │   ├── ranking/      ranking_screen, ranking_provider
│   │   ├── announcements/ announcement_list_screen, announcement_detail_screen, announcement_provider
│   │   ├── instruments/  instrument_catalog_screen, instrument_detail_screen, recommendation_strip, instrument_provider
│   │   ├── rentals/      rental_list_screen, rental_detail_screen, rental_request_sheet, rental_timeline, rental_provider
│   │   ├── payments/     payment_screen, payment_status_view, payment_provider
│   │   ├── notifications/ notification_inbox_screen
│   │   └── profile/      profile_screen, edit_profile_screen, change_password_screen, membership_card, profile_provider
│   └── widgets/
│       ├── async_state_view.dart     # loading / error+retry / empty / data (§9)
│       ├── paged_list_view.dart      # search field + list + PagedPaginationBar over PagedFetchController
│       ├── mobile_form_scaffold.dart # AppBar with X-close (top-right) + implicit back, save FAB/button
│       ├── blocked_reason_banner.dart# disabled-with-reason surface (membership, unpaid debt)
│       ├── status_chip.dart          # rental / payment / lecture status → Bosnian label + colour
│       ├── section_header.dart
│       └── labeled_value.dart        # thin wrapper over core InfoRow with mobile label width
└── test/                             # see 03-plan.md
```

`Inference`: nothing under `features/` is reusable by the desktop (different screens, different navigation), so nothing there belongs in core.

---

## 3. Split between `enote_core` and `enote_mobile`

### 3.1 Rule applied

`Recommendation`. A file goes to `enote_core` only if it encodes the **API contract** (DTOs, enum wire formats, error shapes, endpoint-level controllers) or is a **screen-agnostic widget** already used by the desktop. Everything with a `BuildContext` that assumes a phone, and every package that only the phone needs (Stripe, SignalR, secure storage, deep links), stays in `enote_mobile` so the desktop's dependency tree does not grow.

### 3.2 Proposed changes to `enote_core` (all additive)

Each item states why it cannot live in the app and the desktop-safety check performed.

| ID | Change | Why core | Desktop safety (`Observed fact` unless noted) |
|---|---|---|---|
| **C1** | New `models/rentals/recommendation_models.dart` with `InstrumentRecommendationDto { InstrumentDto instrument; double score; List<String> reasons; }` + `fromJson`; export from `enote_core.dart`. | It is the wire contract of `GET student/instruments/recommended` (`InstrumentRecommendationDto { Instrument, Score, Reasons }` in `Recommendations/InstrumentRecommendationDto.cs`). All other DTOs live in core; a lone DTO in the app breaks the convention. | No desktop file declares a symbol with this name (no recommendation feature in `enote_desktop/lib`). Export adds a name to the barrel; no clash. |
| **C2** | `ApiErrorMapper.mapError`: after the existing `message` check, if the body has an `errors` object (ASP.NET `ValidationProblemDetails`), join all field messages with a newline and return that; else if `title` is non-empty and `status == 400`, return `title`. `ApiError.fromJson` gains an optional `errors: Map<String, List<String>>` field. | G5: request-validation failures (`RegisterRequest`, `ChangePasswordRequest`, `RsvpRequest`, …) currently collapse to `'Neispravan zahtjev.'`, which violates the "forward backend validation messages" rule for **both** clients. The mapper is the single place that knows the shape. | Existing branches are untouched. Pinned tests: `enote_desktop/test/delegated_user_screens_test.dart:41,322` feed a body `{'message': 'Neispravan zahtjev.'}` (still hits the `message` branch); `enote_core/test/api_response_test.dart:44` expects the default for an empty 400 body (no `errors` key → unchanged). `Inference`: both stay green. Run `flutter test` in both packages to confirm (03-plan). |
| **C3** | `NotificationController`: add `int _page = 1; bool _hasMore = false; bool get hasMore; Future<void> loadMore()` that fetches `page + 1` with the same `pageSize` and appends; `refresh()` resets `_page = 1` and sets `_hasMore = items.length == pageSize`. | The inbox on a phone needs "load more" while the same controller drives the badge; a second controller would desync the count (the very thing the class comment warns about). | Purely additive; `refresh()`'s observable behaviour (one page, `notifyListeners`) is unchanged; desktop never calls `loadMore`. No desktop test references the private fields. |
| **C4** (optional) | Fix the typo in the 401 default message: `'istečla'` → `'istekla'`. | User-facing string shared by both clients. | `Open question`: no test pin for `'istečla'` was found by grep in `UI/*/test`; confirm before changing. Skip if any test pins it. |

Nothing else changes in core. In particular **no** SignalR client, **no** secure-storage adapter, **no** theme file and **no** `ProfileProvider` move (see 3.3).

### 3.3 Things deliberately kept in the app, with the better alternative stated

- **Theme tokens.** The desktop's `AppTheme` is in `enote_desktop/lib/theme`, not core (G15). Default: `enote_mobile/lib/theme/app_theme.dart` re-declares the same colour/typography/input tokens (≈140 lines). `Recommendation` (better alternative): move `AppTheme` into `enote_core/lib/theme/` and change one import line in `enote_desktop/lib/main.dart`. Trade-off: single source of truth for both clients, at the cost of one line in the "finished" desktop, which brief rule 2 forbids. Keep the copy unless the user lifts rule 2.
- **`ProfileProvider`.** Desktop's `features/profile/profile_provider.dart` (plain class over `users/me`, five calls) is exactly what mobile needs. Default: mobile writes its own `ProfileProvider` with the same five methods. `Recommendation` (better alternative): promote it to core. It **cannot** simply be added to core under the same name — desktop `main.dart` imports both the core barrel and its local file, so a same-named core export would be an ambiguous import (`Inference` from Dart import rules). Promotion therefore requires deleting the desktop file and fixing its imports — again a desktop change. Trade-off: ~40 duplicated lines vs. touching the desktop.
- **401 interception.** Done in mobile via `SessionHttpClient extends http.BaseClient` injected into both `AuthState(httpClient:)` and `ApiClient(httpClient:)` (both accept one, G2/G3). Alternative: an `onUnauthorized` callback on core `ApiClient`. Trade-off: the core callback would also cover `AuthState.login` only if `AuthState` got the same hook; the wrapper client covers both with zero core surface and is the default.
- **Paging on phone.** Default: reuse `PagedFetchController` + `PagedPaginationBar` (page N of M, previous/next) inside a mobile `PagedListView`. Alternative: an infinite-scroll controller (append pages on scroll). Trade-off: infinite scroll is nicer to thumb but is ~120 new lines and a second paging state machine; the explicit bar already satisfies "pagination on every list" and is tested.

---

## 4. State management and dependency injection

`Recommendation`, following G15 exactly (`provider` + `ChangeNotifier`, one `MultiProvider`, no second state library).

### 4.1 Bootstrap (`main.dart`)

```
1. WidgetsFlutterBinding.ensureInitialized()
2. tokenStore = TokenStore(); await tokenStore.load()          // async secure-storage read, once
3. sessionHttp = SessionHttpClient(onUnauthorized: () => authState.logout(), timeout: 20 s)
4. authState = AuthState(baseUrl: kApiBaseUrl,
        tokenReader: tokenStore.read,                            // sync: returns the in-memory mirror
        tokenWriter: tokenStore.write,                           // sync signature; persists fire-and-forget
        httpClient: sessionHttp)
5. apiClient = ApiClient(baseUrl: kApiBaseUrl, authState: authState, httpClient: sessionHttp)
6. Stripe.publishableKey = kStripePublishableKey (flutter_stripe)   // guarded: skip if empty
7. runApp(EnoteMobileApp(authState, apiClient, tokenStore))
```

`Inference`: because `AuthState` reads the token synchronously in its constructor (G2), the secure-storage read must complete **before** step 4; the `TokenStore` keeps a `String? _cached` that the sync hooks use, and `write` schedules the async persist. This satisfies rule 4 of the brief without changing core.

### 4.2 `MultiProvider` composition

| Provider | Kind | Lifetime | Created |
|---|---|---|---|
| `AuthState` | `ChangeNotifierProvider.value` | app | eagerly in `main` |
| `ApiClient` | `Provider.value` | app | eagerly in `main` |
| `TokenStore` | `Provider.value` | app | eagerly in `main` |
| `SessionController` | `ChangeNotifierProvider(create:, lazy: false)` | app | eager — `SessionGate` reads it on first frame; holds `UserProfileResponse?`, `membershipPaidUntil`, `isMembershipActive`, `logoutAndRevoke()`, `reloadProfile()` |
| `NotificationController(endpoint: 'student/notifications')` | `ChangeNotifierProvider(create:, lazy: false)` | app | eager — `RootShell` starts polling in `didChangeDependencies`, mirroring `MasterScreen` |
| `NotificationHubClient` | `Provider(create:, dispose:)` | app | lazily created by `RootShell`; started/stopped with auth changes (§8) |
| `CourseProvider`, `LectureProvider`, `AssignmentProvider`, `AnnouncementProvider`, `InstrumentProvider`, `RentalProvider` | `ChangeNotifierProvider(create:)` | app | lazy (provider default) — first `context.read` |
| `RankingProvider`, `PaymentProvider`, `ProfileProvider` | `Provider(create:)` (plain classes, no listeners) | app | lazy |
| `LectureNoteProvider(lectureId)` | `ChangeNotifierProvider` **scoped to the route** | screen | created in `LectureDetailScreen` when opening notes — same reasoning as the desktop comment in `main.dart:102-105` (endpoint depends on the lecture) |

`Inference`: keeping feature providers lazy means a cold start performs exactly three network calls before the first screen (profile, unread count, notification page 1), all via `Future.wait` in `SessionController.bootstrap()` (§6.5).

### 4.3 Provider shapes (what each class extends)

| Class | Extends | Endpoint (relative to `kApiBaseUrl`) | Extra methods |
|---|---|---|---|
| `CourseProvider` | `ReadOnlyProvider<CourseDto>` | `student/courses` | `enroll(id)` → POST `student/courses/{id}/enroll`; `unenroll(id)`; both `throwIfError` + `notifyListeners` |
| `LectureProvider` | `ReadOnlyProvider<LectureDto>` | `student/lectures` | `rsvp(id, RsvpRequest)` → `RsvpResponse` |
| `LectureNoteProvider` | `ReadOnlyProvider<LectureNoteDto>` | `student/lectures/{lectureId}/notes` | — |
| `AssignmentProvider` | `ReadOnlyProvider<AssignmentDto>` | `student/assignments` | `submit(id, bytes, fileName, contentType)` → `postMultipart('student/assignments/{id}/submit')` → `AssignmentSubmissionDto` |
| `AnnouncementProvider` | `ReadOnlyProvider<AnnouncementDto>` | `student/announcements` | — |
| `InstrumentProvider` | `ReadOnlyProvider<InstrumentDto>` | `instruments/public` | `recommended({count = 5})` → `List<InstrumentRecommendationDto>` (C1); `recordView(id)` → POST, errors swallowed (analytics signal, never blocks UI) |
| `RentalProvider` | `ReadOnlyProvider<InstrumentRentalDto>` | `student/rentals` | `createRequest(RentalCreateRequest)`, `cancel(id, note?)`, `hasUnpaidDebt()` (§6.4) |
| `RankingProvider` | plain | `student/courses/{courseId}/ranking` | `getForCourse(courseId)` → `List<CourseRankingEntryDto>` (not paged: server returns a list) |
| `PaymentProvider` | plain | `student/rentals/{rentalId}/payments` | `createIntent(rentalId)` → `CreatePaymentIntentResponse`; `status(rentalId)` → `RentalPaymentDto` |
| `ProfileProvider` | plain | `users/me`, `users/me/password`, `users/me/picture` | same five calls as desktop (3.3) |
| `AuthProvider` (thin) | plain | `auth/register`, `auth/forgot-password`, `auth/reset-password` | anonymous POSTs via `ApiClient` (no token yet → no header, G3) |

`Inference`: `ReadOnlyProvider.getById` builds `'$endpoint/$id'`, which matches every student detail route in §6.1, so no override is needed.

---

## 5. Navigation architecture

### 5.1 Shell

`Recommendation`. `SessionGate` (a `Consumer<AuthState>`) renders:

1. `LoginScreen` when `!isAuthenticated`;
2. `RoleBlockedScreen` when authenticated but `!hasRole('Student')` (G2 exposes `hasRole`); copy: *"Ova aplikacija je namijenjena studentima. Prijavite se na desktop aplikaciju."* + "Odjavi se". The app never renders an empty shell for an admin/instructor/employee token (brief §6, last bullet);
3. `RootShell` otherwise.

`RootShell` = `Scaffold` with a Material 3 `NavigationBar` of **four** destinations and an `AppBar` whose trailing action is core `NotificationBadge` (opens the inbox). Destinations:

| Index | Label (BS) | Root screen | Contains |
|---|---|---|---|
| 0 | Instrumenti | `InstrumentCatalogScreen` | recommendation strip + paged catalogue |
| 1 | Učenje | `LearningTabsScreen` (top `TabBar`: **Kursevi · Predavanja · Zadaci · Objave**) | the four academic lists |
| 2 | Iznajmljivanja | `RentalListScreen` | order history |
| 3 | Profil | `ProfileScreen` | profile, membership card, change password, logout |

Each destination keeps its own `Navigator` (nested) so a back gesture inside "Učenje" pops to the tab list, not to another tab. Detail screens push onto the nested navigator; the AppBar of every pushed screen shows the automatic back arrow **and** an explicit "X" close action on form screens (`MobileFormScaffold`), which is the mobile equivalent of the desktop's dialog X.

### 5.2 Route table (`app_router.dart`)

Named routes with typed argument classes; pushed on the nested navigator of the active tab unless marked *root*.

| Route name | Screen | Args | Notes |
|---|---|---|---|
| `/courses/detail` | `CourseDetailScreen` | `courseId` | **master–details** (course + its lectures) |
| `/lectures/detail` | `LectureDetailScreen` | `lectureId` | RSVP sheet, notes entry |
| `/lectures/notes` | `LectureNoteListScreen` | `lectureId` | scoped `LectureNoteProvider` |
| `/lectures/notes/detail` | `LectureNoteDetailScreen` | `lectureId, noteId` | |
| `/assignments/detail` | `AssignmentDetailScreen` | `assignmentId` | submit |
| `/courses/ranking` | `RankingScreen` | `courseId` | |
| `/announcements/detail` | `AnnouncementDetailScreen` | `AnnouncementDto` | already loaded, pass the object |
| `/instruments/detail` | `InstrumentDetailScreen` | `instrumentId` | records a view on open |
| `/rentals/detail` | `RentalDetailScreen` | `rentalId` | timeline + pay entry point |
| `/rentals/pay` | `PaymentScreen` | `rentalId` | PaymentSheet |
| `/notifications` (*root*) | `NotificationInboxScreen` | — | pushed on the root navigator so it overlays any tab |
| `/profile/edit` | `EditProfileScreen` | — | |
| `/profile/password` | `ChangePasswordScreen` | — | separate action, old password required |
| `/auth/register`, `/auth/forgot`, `/auth/reset` (*root*) | | `email?, token?` for reset | reachable while logged out |

### 5.3 Deep links — what arrives

- **Password reset:** the backend appends `?email=&token=` to `Smtp:PasswordResetUrl` (G9). `Recommendation`: set `SMTP_PASSWORD_RESET_URL=enote://reset-password` in `.env`. The composed link `enote://reset-password?email=…&token=…` matches the Android intent filter below. Trade-off vs. an `https://` App Link: App Links need a hosted `assetlinks.json` on a real domain; a custom scheme works on the emulator with no infrastructure. Downside: some webmail clients do not render custom-scheme links as clickable — the manual script in 03-plan tests via `adb shell am start -a android.intent.action.VIEW -d "enote://reset-password?email=…&token=…"`.
- **Stripe return:** PaymentSheet handles card + 3-D Secure inside the app (G18: PaymentSheet supported). `Recommendation`: pass `returnURL: 'enote://stripe-redirect'` in the PaymentSheet parameters so redirect-based methods (if ever enabled on the Stripe account) come back to the app; the handler treats it as a no-op re-entry and re-polls payment status (§6.4). `Open question`: whether the Stripe test account has any redirect-based method enabled; with card-only it never fires.

### 5.4 Deep links — handling (cold start **and** resumed)

`DeepLinkHandler` (in `session/`):

1. Subscribes to `AppLinks().uriLinkStream` once, from `EnoteMobileApp.initState`. `Observed fact` (pub.dev): the stream replays the initial link, so cold-start and warm-resume both arrive on the same stream.
2. Each URI is normalised into a `DeepLinkTarget` (`resetPassword(email, token)` | `stripeRedirect` | `unknown`).
3. **Navigator readiness:** on cold start the first event can precede the first frame. The handler stores a `pendingTarget`; `EnoteMobileApp` flushes it in a post-frame callback via the global `navigatorKey`. `Inference`: without this queue the cold-start link is lost.
4. `resetPassword` pushes `/auth/reset` on the **root** navigator with `pushNamedAndRemoveUntil` regardless of auth state (a logged-in user may still reset). On success: `authState.logout()` (local), snack *"Lozinka je uspješno promijenjena. Prijavite se novom lozinkom."*, land on `LoginScreen`.
5. `stripeRedirect`: if a `PaymentScreen` is on top, call its `recheck()`; otherwise ignore.
6. Android manifest: one `<intent-filter>` with `android.intent.action.VIEW`, `BROWSABLE`/`DEFAULT` categories, `<data android:scheme="enote" />`; `android:launchMode="singleTask"` on the activity so a resumed link does not spawn a second instance (`Inference` from Android intent semantics; verify on the emulator in 03-plan).

---

## 6. API layer

### 6.1 Provider → endpoint map (student surface; all routes prefixed by `kApiBaseUrl` = `…/api/v1/`)

Verified against the controllers listed in §1. Query names are the C# property names in camelCase (ASP.NET binds case-insensitively; `Inference`).

| Screen / action | Method + path | Query / body | Provider |
|---|---|---|---|
| Login | POST `auth/login` | `{username, password}` | `AuthState.login` (core) |
| Register | POST `auth/register` | `RegisterRequest` | `AuthProvider` → then `AuthState` adopts the returned token (`Open question`: `AuthState` has no public "adopt token" method; default = call `AuthState.login(username, password)` right after a successful register, one extra round-trip; alternative = core method `AuthState.applyToken(String)`, additive) |
| Forgot / reset password | POST `auth/forgot-password` / `auth/reset-password` | `{email}` / `{email, token, newPassword}` | `AuthProvider` |
| Logout | POST `auth/logout` then local clear | — | `SessionController.logoutAndRevoke` |
| Profile | GET/PUT `users/me`; PUT/GET/DELETE `users/me/picture`; PUT `users/me/password` | `UpdateProfileRequest`, multipart `file`, `ChangePasswordRequest` | `ProfileProvider` |
| Courses list | GET `student/courses` | `page, pageSize, includeTotalCount, name` | `CourseProvider.getPage` |
| Course detail | GET `student/courses/{id}` | — | `CourseProvider.getById` |
| Enrol / unenrol | POST `student/courses/{id}/enroll` / `…/unenroll` | — (204) | `CourseProvider` |
| Ranking | GET `student/courses/{courseId}/ranking` | — | `RankingProvider` |
| Lectures (all / per course) | GET `student/lectures` | `courseId, name, lectureType, from, to, page…` | `LectureProvider.getPage` — the **details** half of the master–details |
| Lecture detail | GET `student/lectures/{id}` | — | `LectureProvider.getById` |
| RSVP | POST `student/lectures/{id}/rsvp` | `{confirm, note?}` → `{lectureId, studentId, confirmed}` | `LectureProvider.rsvp` |
| Notes | GET `student/lectures/{lectureId}/notes[/{noteId}]` | `title, page…` | `LectureNoteProvider` |
| Assignments | GET `student/assignments[/{id}]` | `title, dueAfter, dueBefore, page…` | `AssignmentProvider` |
| Submit | POST `student/assignments/{id}/submit` (multipart `file`, ≤ 5 MB) | → `AssignmentSubmissionDto` | `AssignmentProvider.submit` |
| Own submission | GET `student/assignments/{id}/submission` | — (404 when none) | `AssignmentProvider.mySubmission` |
| Submission history | GET `student/submissions` | `page, pageSize, includeTotalCount` | `AssignmentProvider.myHistory` |
| Announcements | GET `student/announcements` | `page, pageSize, includeTotalCount, title` | `AnnouncementProvider` |
| Events | GET `student/events` | `page, pageSize, includeTotalCount, from, to` | `EventProvider` |
| Catalogue | GET `instruments/public[/{id}]` (anonymous) | `search, instrumentTypeId, isAvailable, page…` | `InstrumentProvider` |
| Recommendations | GET `student/instruments/recommended?count=5` | → `List<InstrumentRecommendationDto>` | `InstrumentProvider.recommended` |
| Record view | POST `student/instruments/{id}/view` | — (204) | `InstrumentProvider.recordView` |
| Rentals | GET `student/rentals[/{id}]` | `rentalStatus` (int 1–7), `instrumentId`, `page…` | `RentalProvider` |
| Debt check | GET `student/rentals/debt` | → `{hasUnpaidDebt, rentalId?}` | `RentalProvider.debt` |
| Request rental | POST `student/rentals` | `{instrumentId, note?}` → 201 `InstrumentRentalDto` | `RentalProvider.createRequest` |
| Cancel | POST `student/rentals/{id}/cancel` | `{note?}` | `RentalProvider.cancel` |
| Pay | POST `student/rentals/{id}/payments/create-intent`; GET `student/rentals/{id}/payments` | → `CreatePaymentIntentResponse` / `RentalPaymentDto` | `PaymentProvider` |
| Notifications | GET `student/notifications`, `…/unread-count`; PATCH `…/{id}/read`, `…/read-all` | `isRead, page, pageSize` | `NotificationController` (core) |
| Images | GET `uploads/instruments/{f}`, `uploads/music-stores/{f}` (anon), `uploads/assignments/{f}` (auth) | — | `networkImageOrPlaceholder` with `apiClient` (G13 attaches headers) |

### 6.2 Error mapping

`Recommendation`: every provider call ends in `decodeOrThrow` / `throwIfError` (core), so screens catch **one** type and show `userMessage(e)`:

- inline `ErrorBanner` (core widget) below the form's submit button for form screens;
- `ErrorBanner.show` snackbar for list actions (cancel, mark-read, RSVP);
- full-screen `AsyncStateView.error` with "Pokušaj ponovo" for first loads.

With C2 in place, FluentValidation field errors reach the user verbatim (Bosnian, from `*RequestValidator.WithMessage`). Business errors (`RentalUnpaidDebt`, membership, `PaymentNotPayableInStatus`) already arrive as `{message}` and pass through today (G5).

### 6.3 401 handling and session expiry

`Recommendation`. `SessionHttpClient.send()`:

1. wraps the inner `http.Client().send()` in `.timeout(20 s)` (a `TimeoutException` becomes the core "cannot reach server" message via `userMessage`);
2. if the response status is **401** and the request path does **not** start with `${kApiBaseUrl}auth/`, invokes `onUnauthorized()` exactly once per token (guarded by remembering the token that failed) and still returns the response so the caller's `throwIfError` surfaces `'Vaša sesija je istekla…'`;
3. `onUnauthorized` → `authState.logout()` → `SessionGate` swaps to `LoginScreen` on the next frame, with a one-shot banner *"Vaša sesija je istekla. Prijavite se ponovo."*.

`Inference`: the `auth/` exclusion is required because a wrong password is also a 401 (`AuthController.Login` documents 401) and must not trigger the logout path. Revoked tokens (G8) and expired tokens both land here; there is no refresh endpoint to try (G8), so "redirect to login" is the only correct branch of the course rule.

On launch, `AuthState.isAuthenticated` already returns false for an expired stored token (G2), so the app opens on `LoginScreen` without a network call.

### 6.4 Domain checks that need composed calls

- **Unpaid-debt pre-check** (blocked-by-unpaid-rental state). **Revised 2026-09-09 — the endpoint was built, so the two-call approximation is dropped.** `RentalProvider.debt()` = one `GET student/rentals/debt` → `{hasUnpaidDebt, rentalId?}`. `rentalId` is the earliest unpaid Completed/ReturnedEarly rental, so the blocking banner deep-links straight to that rental's payment screen. The server remains authoritative and its message is shown verbatim if the client and server ever disagree.
- **Membership gate:** `SessionController.isMembershipActive = membershipPaidUntil != null && membershipPaidUntil!.toUtc().isAfter(DateTime.now().toUtc())`. Drives "Upiši se" and "Zatraži iznajmljivanje" being disabled with the reason *"Članarina je istekla {datum}. Obratite se školi za obnovu."* / *"Članarina nije aktivna."* `Open question`: the exact server boundary (is the expiry day itself still valid?) was not read; the client uses strict "after now" and always surfaces the server message if the server disagrees.
- **Payment finalisation:** after `Stripe.instance.presentPaymentSheet()` resolves without throwing, `PaymentProvider.status(rentalId)` is polled up to 5 × 2 s until `status == succeeded`, then `RentalProvider.getById` is refetched to flip `isPaid`. If still `requiresAction` after the window: show *"Plaćanje se obrađuje. Provjerite ponovo za nekoliko sekundi."* with a retry button — never a false "Plaćeno". `Inference` from G7/G10: there is no push for payment success, so polling is the only signal.

### 6.5 `Future.wait()` sites (independent calls only)

| Screen | Parallel calls |
|---|---|
| Session bootstrap | `users/me` ‖ `student/notifications/unread-count` ‖ `student/notifications` page 1 |
| Instrumenti tab | `student/instruments/recommended` ‖ `instruments/public` page 1 ‖ `hasUnpaidDebt()` |
| Course detail | `student/courses/{id}` ‖ `student/lectures?courseId=` page 1 ‖ `student/courses/{id}/ranking` |
| Rental detail | `student/rentals/{id}` ‖ `student/rentals/{id}/payments` (404 = no payment yet, treated as "not started") |
| Profile | `users/me` (picture loads via `Image.network`, never base64 in `build`) |
| Assignment detail | `student/assignments/{id}` only (see gap A3 in §11) |

Sequential where dependent: create-intent → PaymentSheet → status poll; rsvp → refetch lecture.

---

## 7. Auth and session

| Concern | Decision (`Recommendation`) |
|---|---|
| Token storage | `flutter_secure_storage` key `enote.access_token` (Android Keystore-backed). `TokenStore.load()` once at boot; `read()` sync from the mirror; `write(token)` updates the mirror, then `await storage.write/delete` fire-and-forget with error logging. |
| Restoration on launch | Handled by `AuthState` constructor via `tokenReader` (G2); expired token → `LoginScreen`; valid → `RootShell` → `SessionController.bootstrap()`. |
| Logout / revocation | `SessionController.logoutAndRevoke()`: `try { await apiClient.post('auth/logout') } catch (_) {}` (best effort, offline logout must still work), then `authState.logout()` (clears + `tokenWriter(null)` → secure storage delete), `notificationController.stopPolling()`, `hubClient.stop()`. Confirmation dialog first (core `confirmDialog`, *"Odjava"* / *"Da li ste sigurni da se želite odjaviti?"*). |
| Role guard | `SessionGate` step 2 (§5.1). Roles come from the JWT only; `is_manager` is ignored on mobile. |
| Register | `RegisterScreen` → `AuthProvider.register` → on 200 call `AuthState.login(username, password)` (see 6.1 open question) → `SessionGate` routes to the shell. Client-side validation mirrors G14 (username required, email format via `Validators.email`, password via `Validators.password` — stricter than the server's ≥ 8, which is acceptable because server rules are a subset). |
| Identity in requests | Never in a route or body; every student endpoint derives the user from the JWT (verified: no student route takes a user id). |

---

## 8. Real-time notifications

`Recommendation`: **SignalR is primary, polling is the always-on fallback.** Both run whenever the user is authenticated.

Why both rather than one:

- The course rule mandates auto-refresh via SignalR **or** polling; a SignalR-only client that fails to connect on an emulator (proxy, TLS, `10.0.2.2` WebSocket quirks) silently becomes manual-refresh — an explicit fail. Keeping the existing 30 s unread-count poll (G6, ~200 bytes per tick) guarantees the rule even when the hub is down.
- The hub payload has no `id`/`isRead` (G6/G7), so a push cannot be inserted into the list locally anyway; the correct reaction is `notificationController.refresh()` (one page fetch) — the hub therefore only makes the refresh **instant** instead of ≤ 30 s late.

`NotificationHubClient` (mobile, `signalr_netcore`):

- URL = origin of `kApiBaseUrl` + `/hubs/notifications` (derived the same way `networkImageOrPlaceholder` derives the origin, G13).
- `accessTokenFactory: () async => authState.accessToken`; `Inference`: the client sends it as `?access_token=` for WebSocket transport, which is exactly what the backend accepts on `/hubs` paths (G7). `Open question`: `signalr_netcore` docs do not state the query-string mechanism explicitly; verified empirically in 03-plan's script (server log line *"NotificationHub connection … has no valid 'Sub' claim"* is the failure signature).
- `withAutomaticReconnect()`; `on('ReceiveNotification', …)` → parse `NotificationPushDto.fromJson` → `notificationController.refresh()` + a foreground `SnackBar` with the title (tap → inbox).
- Lifecycle: `start()` when `authState.isAuthenticated && hasRole('Student')`, `stop()` on logout; `WidgetsBindingObserver` pauses the hub in `AppLifecycleState.paused` and restarts on `resumed` (polling keeps running only while the app is foregrounded; background delivery is out of scope — no FCM in the brief).

Risk: `signalr_netcore` was last published ~12 months ago (G18). Fallback if it fails to build on Flutter 3.47: `signalr_core` (`Open question`, version not verified) or polling-only (already compliant). Decide at task time, not now.

---

## 9. Offline / empty / loading / error strategy (uniform)

`Recommendation`: one widget, `AsyncStateView<T>`, wraps every first load:

| State | Rendering | Copy |
|---|---|---|
| loading (first) | centred `CircularProgressIndicator` | — |
| loading (refresh) | `RefreshIndicator` on lists; inline progress on buttons | — |
| error | icon + `userMessage(e)` + `OutlinedButton('Pokušaj ponovo')` | core messages; offline → *"Nije moguće povezati se sa serverom. Pokušajte ponovo."* |
| empty | icon + one sentence + optional CTA | e.g. *"Nema rezultata za pretragu."*, *"Još nemate iznajmljivanja."*, *"Nema obavještenja."* |
| data | child | — |

Rules applied everywhere: destructive/irreversible actions (odjava, otkazivanje zahtjeva, plaćanje, slanje zadaće, ispis sa kursa, brisanje slike) go through core `confirmDialog`; disabled actions always render a `BlockedReasonBanner` with the reason instead of vanishing; after a successful create the list is re-fetched from page 1 (newest first per server ordering) with no manual refresh. No offline cache is built — the course rules do not ask for one and the app is read-mostly; every screen is re-fetchable with pull-to-refresh.

---

## 10. Proposed `pubspec.yaml`

Versions: `verified` = read from a lock file in this repo or from pub.dev today (G17/G18); `unverified` = not read.

```yaml
name: enote_mobile
description: eNoteV2 student mobile client.
publish_to: 'none'
version: 1.0.0+1

environment:
  sdk: ^3.13.1            # verified — matches enote_desktop

dependencies:
  flutter:
    sdk: flutter
  enote_core:
    path: ../enote_core     # shared contract + widgets (brief rule 1)
  provider: ^6.1.5          # verified (lock 6.1.5+1) — established state pattern, no second library
  http: ^1.6.0              # verified (lock 1.6.0) — needed for the SessionHttpClient BaseClient wrapper
  file_picker: ^10.3.10     # verified (lock 10.3.10) — assignment file + profile picture picking (one picker for both; avoids an unverified image_picker)
  flutter_secure_storage: ^11.0.0   # verified (pub.dev 2026-09-09) — token persistence; raises minSdk to 23
  flutter_stripe: ^14.0.0   # verified (pub.dev 2026-09-09) — PaymentSheet, in-app; needs FlutterFragmentActivity + AppCompat theme
  signalr_netcore: ^1.4.4   # verified (pub.dev 2026-09-09) — hub client; see §8 risk
  app_links: ^7.2.1         # verified (pub.dev 2026-09-09) — cold-start + resumed deep links

dev_dependencies:
  flutter_test:
    sdk: flutter
  flutter_lints: ^6.0.0     # verified (lock 6.0.0)
  http: ^1.6.0              # verified — MockClient for provider tests, same as desktop

flutter:
  uses-material-design: true
```

Android: `minSdkVersion 23`, `MainActivity : FlutterFragmentActivity`, launch theme parent `Theme.AppCompat.Light.NoActionBar` (or `.DayNight`), `launchMode="singleTask"`, `enote://` intent filter, `INTERNET` permission. `Inference` from G18 requirements; confirmed by the first `flutter run` in 03-plan.

Configuration (`config.dart`):

```dart
const String kApiBaseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://10.0.2.2:5059/api/v1/');
const String kStripePublishableKey = String.fromEnvironment('STRIPE_PUBLISHABLE_KEY', defaultValue: '');
```

`Recommendation`: the publishable key must be a `--dart-define` because no API endpoint exposes `Stripe__PublishableKey` (G10). Alternative: backend `GET api/v1/payments/config` → `{publishableKey}` (anonymous or Student) — out of scope, listed in §11. Note the default base URL includes `/api/v1/` exactly like the desktop's; the course text's example `http://10.0.2.2:5000` omits it, so the README must show the full value.

---

## 11. Gaps

### 11.1 The seven items from brief §6

| # | Item | Verification | Scope decision |
|---|---|---|---|
| 1 | README stale ("separate repository"; credentials) | **verified** (G16) | **in scope** — corrected as part of the mobile deliverable (03-plan lists the exact edits). |
| 2 | No student-facing Events endpoint | **verified**, then **closed 2026-09-09** | **in scope — built.** `GET student/events` on the new `StudentEventController`, `[Authorize(Roles = Student)]`, backed by `EventService.GetPagedForStudentAsync`: `CourseId == null` (platform-wide) OR an actively-enrolled course, then the existing `ApplySearch` for `from`/`to`. `EventDto` is reused as-is — see the caveat under A9 below. Mobile renders it as a **Događaji** sub-tab inside Učenje, not a fifth bottom-nav destination. |
| 3 | No student music-store browse endpoint | **verified** | **closed as `not needed` by decision, 2026-09-09.** No course rule requires a store page, and pickup is arranged through the rental itself, so a student never needs store contact details. The catalogue keeps rendering `InstrumentDto.musicStore` as plain, unclickable text. If reversed: `GET api/v1/stores/public` (paged `MusicStoreDto` with image path) and `GET api/v1/stores/public/{id}`, `[AllowAnonymous]`. |
| 4 | Rental timeline must be assembled client-side; actor fields are raw IDs | **verified** — `InstrumentRentalDto` has `approvedById`/`rejectedById` (int?) and no actor names | **in scope, no backend change**: the timeline is built from `requestedAt / approvedAt / rejectedAt / pickedUpAt / returnedAt / paidAt`, and the actor of every store-side step is rendered as the **store** (`storeName` is in the DTO), never a person and never an id. Layout in 02-design. |
| 5 | Membership is visible but not renewable by the student | **verified** — `UserProfile.membershipPaidUntil` on `users/me`; renewal only under admin routes | **in scope** — explained blocking state (§6.4), no backend change. |
| 6 | Stripe keys are placeholders | **verified** in `.env.docker.example` (`sk_test_...`, `whsec_...`); the real `.env` was deliberately not read (`Open question`: its contents) | **in scope** as run-time prerequisite (03-plan): real `sk_test`/`whsec` on the server, `pk_test` via `--dart-define`, `stripe listen --forward-to localhost:5059/api/v1/payments/stripe/webhook`. |
| 7 | `is_manager` / four-role `topRole` in shared `AuthState` | **verified** (G2) | **in scope** — `RoleBlockedScreen` (§5.1); no core change. |

### 11.2 Additional gaps found while verifying (not in the brief)

| # | Gap | Effect on mobile | Decision |
|---|---|---|---|
| A1 | `CourseDto` carried no per-student enrolment flag (G12) | The course detail could not choose "Upiši se" vs "Ispiši se" | **closed 2026-09-09.** `CourseDto.IsEnrolled` + `CourseSearchObject.EnrolledOnly` (bool?). Populated only in `GetPagedForStudentAsync` / `GetByIdForStudentAsync`; admin and instructor routes still return `false`. The *"Status upisa nije dostupan."* fallback is no longer needed and must not ship. |
| A2 | `LectureDto` carried no current-student attendance status (G12) | RSVP buttons could not reflect the existing answer | **closed 2026-09-09.** `LectureDto.MyAttendanceStatus`, populated from the caller's own `Attendances` row on the two student paths (both already `.Include(x => x.Attendances)`); `null` on every other route, including the instructor paged route. |
| A3 | No student endpoint for own submission / grade (G12) | The graded/ungraded split had no data source; the grade was visible only in the notification body (*"Ocjena: N"*) and the ranking average | **closed 2026-09-09.** `GET student/assignments/{id}/submission` (404 through `NotFoundException`, so the standard `{status, code, message}` shape) and `GET student/submissions` (paged, `SubmittedAt` desc, then `Id` desc). Ownership from the `sub` claim only. `AssignmentSubmissionSearchObject` carries page fields only — no filters were specified and none were added. |
| A4 | Validation errors use `ValidationProblemDetails`, unparsed by core (G5) | Field-level messages become *"Neispravan zahtjev."* | **in scope** via core change **C2** (no backend change). |
| A5 | `student/announcements` had no search parameter (G11) | The list could be paged but not searched, against the course rule | **closed 2026-09-09.** `AnnouncementSearchObject.Title` (string?), applied server-side as `Contains` in `StudentAnnouncementFeedService`. Mobile ships the announcement list **with** a search field like every other list. Case sensitivity follows the database collation, same as every other `Contains` search in this codebase. |
| A6 | No debt-status endpoint (G10) | Pre-check was a two-call approximation, wrong above 20 completed rentals, and could not name the rental owed | **closed 2026-09-09.** `GET student/rentals/debt` → `RentalDebtDto { hasUnpaidDebt, rentalId? }`. Predicate matches the create guard in `RentalCommandService` exactly — `(Completed \| ReturnedEarly) && !IsPaid` — ordered by `RequestedAt` then `Id`, so `rentalId` is the earliest debt and the banner deep-links to it. The client approximation in §6.4 is deleted. |
| A7 | Currency label: core `formatKM` renders fees as `KM`, while Stripe charged in `STRIPE_CURRENCY` (`eur` by default) | The pay screen would show "120.00 KM" next to a PaymentSheet saying "€120.00" | **closed 2026-09-09 by decision: `bam` everywhere.** Flipped in all eight places — `StripeOptions.cs` initializer, `DependencyInjection.BuildStripeOptions` fallback, `appsettings.json`, `appsettings.Development.json`, both `docker-compose.yml` `:-eur` defaults, `.env`, `.env.docker.example`. Mobile therefore uses `formatKM` on **every** screen including the pay screen; no screen-specific currency logic. `Open question` remaining: whether the Stripe test account accepts `bam` as a presentment currency — unverifiable without real keys. If it rejects it, stop and report; do not silently fall back. |
| A9 | `EventDto` is shared, but the Dart and C# notions of "platform-wide" differ | The server's student filter is `CourseId == null`; the Dart getter at `communication_models.dart:173` is `courseId == null && instructorId == null`. An event with no course but an assigned instructor is returned by `student/events` yet reports `isPlatformWide == false` on the client | **new, found 2026-09-09 while verifying M4.** Not a server bug — the filter is correct. Mobile must **not** use `EventDto.isPlatformWide` to categorise the student feed; categorise on `courseId` alone, or the "platform-wide" label will be wrong for instructor-owned uncoursed events. No backend change. |
| A8 | No "payment succeeded" push (G7) | Success is learned by polling | in scope (§6.4). |

---

## 12. Alternatives register (one line each; defaults are the brief's or this document's)

| Topic | Default | Better alternative | Trade-off |
|---|---|---|---|
| Theme | copy tokens into mobile | move `AppTheme` to core | one desktop import line vs. duplication |
| `ProfileProvider` | mobile duplicate | promote to core | desktop file deletion vs. ~40 duplicated lines |
| 401 handling | `SessionHttpClient` wrapper (mobile) | `onUnauthorized` on core `ApiClient` | zero core surface vs. shared behaviour for desktop too |
| Paging UX | `PagedFetchController` + page bar | infinite scroll | tested reuse vs. nicer thumb UX and new state machine |
| Register → session | call `login` after `register` | core `AuthState.applyToken()` | one extra round-trip vs. a small additive core method |
| Reset link | custom scheme `enote://` | https App Link | no infrastructure vs. clickable in every mail client |
| Real-time | SignalR + polling both on | SignalR only | guaranteed compliance vs. fewer requests |
| Publishable key | `--dart-define` | `GET payments/config` | no backend work vs. one source of truth |

---

## 13. Open questions (consolidated)

**Still open**

1. `AuthState` has no method to adopt a token returned by `auth/register`; confirm whether adding `applyToken` to core is acceptable or the login round-trip is preferred (§6.1).
3. `signalr_netcore` query-string token behaviour and build health on Flutter 3.47 (§8).
6. Contents of the real `.env` — Stripe sandbox keys and `SMTP_PASSWORD_RESET_URL`. Still placeholders in the repository, so the `bam` presentment-currency check under A7 cannot run yet.
8. Server-side maximum `pageSize` — none found; the client caps at 50.

**Resolved 2026-09-09**

2. ~~Whether the desktop/core tests pin `'istečla'`.~~ **Answered: no.** The string occurs once, at `api_error_mapper.dart:66`, and no test in `enote_core` or `enote_desktop` pins it. C4 is safe to apply.
4. ~~Membership expiry boundary.~~ **Decided: inclusive.** A membership dated today is still valid today; the client compares against the end of that calendar day. The server stays authoritative if it ever disagrees.
5. ~~Currency label policy KM vs EUR.~~ **Decided: KM everywhere,** implemented by setting the Stripe currency to `bam` (A7).
7. ~~Whether students get an events view.~~ **Decided: yes,** and built (§11.1 item 2).

**New question raised by the implementation**

9. `EventService` now takes `IStudentContext? students = null` — an optional constructor parameter added so 24 existing test constructions keep compiling, guarded at the call site by `ArgumentNullException.ThrowIfNull`. It resolves correctly at runtime because `IStudentContext` is registered, but it converts a compile-time requirement into a runtime failure. `Recommendation`: make the parameter required and update the 24 test constructions, or split the student query into its own `StudentEventService`. Either is a small, mechanical change and both are better than the optional parameter. Not urgent — nothing is broken today.
