import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/courses/course_detail_screen.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';
import 'package:enote_mobile/features/lectures/lecture_tile.dart';
import 'package:enote_mobile/features/tuition/tuition_payment_provider.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

Map<String, dynamic> _courseJson({
  String? enrollmentStatus,
  String? enrollmentDecisionNote,
  int? enrollmentId,
  String? paidUntil,
  bool isFree = false,
  bool hasEnded = false,
  int lectureCount = 0,
  bool? hasContentAccess,
}) => {
  'id': 2,
  'instructorId': 3,
  'name': 'Osnove teorije muzike',
  'description': 'Uvod u akorde.',
  'isPublished': true,
  'startDate': '2026-10-01T00:00:00',
  'endDate': '2027-01-31T00:00:00',
  'price': 800.0,
  'enrolledCount': 14,
  'instructorName': 'Amir Hadzic',
  'isEnrolled': enrollmentStatus == 'Active',
  'hasContentAccess':
      hasContentAccess ??
      ((enrollmentStatus == 'Active' || enrollmentStatus == 'Canceled') &&
          (isFree ||
              (paidUntil != null &&
                  !DateTime.parse(paidUntil).isBefore(DateTime.now())))),
  'lectureCount': lectureCount,
  'enrollmentStatus': ?enrollmentStatus,
  'enrollmentDecisionNote': ?enrollmentDecisionNote,
  'enrollmentId': ?enrollmentId,
  'paidUntil': ?paidUntil,
  'isFree': isFree,
  'hasEnded': hasEnded,
};

class _CourseAccess {
  String? paidUntil;

  _CourseAccess(this.paidUntil);
}

const _lectureJson = {
  'id': 11,
  'name': 'Akordi I',
  'location': 'Sala 2',
  'lectureType': 'Theoretical',
  'lectureStatus': 'Scheduled',
  'isCancelled': false,
  'lectureTime': '2026-09-14T18:00:00',
  'duration': 60,
  'capacity': 20,
  'attendeeCount': 5,
  'myAttendanceStatus': 'Pending',
};

const _lecturesPage = {
  'items': [_lectureJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

const _tuitionHistoryPage = {
  'items': [
    {
      'id': 31,
      'enrollmentId': 5,
      'paymentIntentId': 'pi_tuition_1',
      'amountCents': 80000,
      'currency': 'bam',
      'status': 'Refunded',
      'paidAt': '2026-09-09T12:41:00',
      'periodStart': '2026-09-09T12:41:00',
      'periodEnd': '2026-10-09T12:41:00',
      'refundedAt': '2026-10-01T09:00:00',
      'refundedCents': 80000,
    },
  ],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

ScriptedClient _client({
  String? enrollmentStatus,
  String? enrollmentDecisionNote,
  required DateTime paidUntil,
  int? enrollmentId,
  bool isFree = false,
  required _CourseAccess access,
  bool hasEnded = false,
  int lectureCount = 0,
  bool? hasContentAccess,
  String enrollStatus = 'Pending',
}) {
  var status = enrollmentStatus;
  return ScriptedClient((request) {
    final path = request.url.path;
    if (path.endsWith('/tuition/history')) {
      return jsonResponse(_tuitionHistoryPage, 200);
    }
    Object body;
    switch ('${request.method} $path') {
      case 'GET /api/v1/users/me':
        body = meJson(paidUntil: paidUntil);
      case 'GET /api/v1/student/notifications/unread-count':
        body = {'unreadCount': 0};
      case 'GET /api/v1/student/notifications':
        body = {'items': []};
      case 'GET /api/v1/student/courses/2':
        body = _courseJson(
          enrollmentStatus: status,
          enrollmentDecisionNote: enrollmentDecisionNote,
          enrollmentId: status == 'Active' ? enrollmentId : null,
          paidUntil: access.paidUntil,
          isFree: isFree,
          hasEnded: hasEnded,
          lectureCount: lectureCount,
          hasContentAccess: hasContentAccess,
        );
      case 'GET /api/v1/student/lectures':
        body = _lecturesPage;
      case 'POST /api/v1/student/courses/2/enroll':
        status = enrollStatus;
        body = {'enrollmentStatus': enrollStatus};
      case 'POST /api/v1/student/courses/2/unenroll':
        status = 'Canceled';
        body = {'message': 'OK'};
      default:
        body = {'message': 'OK'};
    }
    return jsonResponse(body, 200);
  });
}

class _Harness {
  late final ScriptedClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final SessionController session;
  late final _CourseAccess access;

  Future<void> bootstrap({
    String? enrollmentStatus,
    String? enrollmentDecisionNote,
    DateTime? paidUntil,
    int? enrollmentId,
    bool isFree = false,
    String? coursePaidUntil,
    bool hasEnded = false,
    int lectureCount = 0,
    bool? hasContentAccess,
    String enrollStatus = 'Pending',
  }) async {
    access = _CourseAccess(coursePaidUntil);
    client = _client(
      enrollmentStatus: enrollmentStatus,
      enrollmentDecisionNote: enrollmentDecisionNote,
      paidUntil: paidUntil ?? DateTime.utc(2027, 9, 9),
      enrollmentId: enrollmentId,
      isFree: isFree,
      access: access,
      hasEnded: hasEnded,
      lectureCount: lectureCount,
      hasContentAccess: hasContentAccess,
      enrollStatus: enrollStatus,
    );
    authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      tokenWriter: (_) {},
      httpClient: client,
    );
    apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: NotificationController(
        apiClient: apiClient,
        endpoint: 'student/notifications',
      ),
    );
    await session.bootstrap();
  }

  Widget app() {
    return MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<SessionController>.value(value: session),
        ChangeNotifierProvider<CourseProvider>(
          create: (_) => CourseProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
        Provider<TuitionPaymentProvider>(
          create: (_) => TuitionPaymentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: mobileTheme,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: const CourseDetailScreen(courseId: 2),
      ),
    );
  }
}

void main() {
  testWidgets(
    'not enrolled shows the empty-state copy and no lectures request',
    (tester) async {
      final harness = _Harness();
      await harness.bootstrap();
      await tester.pumpWidget(harness.app());
      await tester.pumpAndSettle();

      expect(
        find.text('Upišite se da vidite predavanja ovog kursa.'),
        findsOneWidget,
      );
      expect(find.textContaining('Akordi I'), findsNothing);
      expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 0);

      await tester.pump(const Duration(seconds: 1));
      expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 0);
    },
  );

  testWidgets('enrolled renders the master card and lecture rows', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      coursePaidUntil: '2027-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Osnove teorije muzike'), findsWidgets);
    expect(find.text('Upisan'), findsOneWidget);
    expect(find.text('800.00 KM'), findsOneWidget);
    expect(find.textContaining('Akordi I'), findsOneWidget);
    expect(find.textContaining('Niste odgovorili'), findsOneWidget);
    expect(
      find.text('Upišite se da vidite predavanja ovog kursa.'),
      findsNothing,
    );
  });

  testWidgets('tap Upi\u0161i se sends a request and shows the pending state', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap();
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(FilledButton, 'Upiši se'));
    await tester.pumpAndSettle();
    expect(find.text('Upis na kurs'), findsOneWidget);
    expect(
      find.text('Želite li se upisati na kurs "Osnove teorije muzike"?'),
      findsOneWidget,
    );

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      countRequests(harness.client, 'POST /api/v1/student/courses/2/enroll'),
      1,
    );
    expect(countRequests(harness.client, 'GET /api/v1/student/courses/2'), 2);
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 0);
    expect(
      find.text(
        'Zahtjev za upis na kurs Osnove teorije muzike je poslan. '
        'Instruktor ga treba odobriti.',
      ),
      findsOneWidget,
    );
    expect(find.text('Na čekanju'), findsOneWidget);
    expect(
      find.text('Zahtjev za upis čeka odobrenje instruktora.'),
      findsOneWidget,
    );
    expect(
      find.widgetWithText(OutlinedButton, 'Otkaži zahtjev'),
      findsOneWidget,
    );
  });

  testWidgets('Ispiši se refetches the master half only, no snackbar', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      coursePaidUntil: '2027-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(OutlinedButton, 'Ispiši se'));
    await tester.pumpAndSettle();
    expect(find.text('Ispis sa kursa'), findsOneWidget);
    expect(
      find.text(
        'Želite li se ispisati sa kursa "Osnove teorije muzike"? '
        'Pristup sadržaju ostaje do 01.01.2027. '
        'Uplaćena školarina se ne vraća.',
      ),
      findsOneWidget,
    );

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      countRequests(harness.client, 'POST /api/v1/student/courses/2/unenroll'),
      1,
    );
    expect(countRequests(harness.client, 'GET /api/v1/student/courses/2'), 2);
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 1);
    expect(
      find.text('Ispisani ste sa kursa. Pristup traje do 01.01.2027.'),
      findsOneWidget,
    );
    expect(
      find.text('Upišite se da vidite predavanja ovog kursa.'),
      findsNothing,
    );
    expect(find.textContaining('Akordi I'), findsOneWidget);
    expect(
      find.text(
        'Zahtjev za upis na kurs Osnove teorije muzike je poslan. '
        'Instruktor ga treba odobriti.',
      ),
      findsNothing,
    );
  });

  testWidgets('canceled with paid time shows the access banner and resumes', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Canceled',
      coursePaidUntil: '2026-07-15T00:00:00Z',
      hasContentAccess: true,
      enrollStatus: 'Active',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(
      find.text('Ispisani ste sa kursa. Pristup traje do 15.07.2026.'),
      findsOneWidget,
    );
    expect(
      find.widgetWithText(FilledButton, 'Nastavi pretplatu'),
      findsOneWidget,
    );
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 1);

    await tester.tap(find.widgetWithText(FilledButton, 'Nastavi pretplatu'));
    await tester.pumpAndSettle();
    expect(find.text('Nastavak pretplate'), findsOneWidget);
    expect(
      find.text(
        'Želite li nastaviti pretplatu na kurs "Osnove teorije muzike"?',
      ),
      findsOneWidget,
    );

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      countRequests(harness.client, 'POST /api/v1/student/courses/2/enroll'),
      1,
    );
    expect(
      find.text('Pretplata na kurs Osnove teorije muzike je nastavljena.'),
      findsOneWidget,
    );
  });

  testWidgets('unenroll on a paid active course keeps access until paidUntil', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      coursePaidUntil: '2027-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(OutlinedButton, 'Ispiši se'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Pristup sadržaju ostaje do'), findsOneWidget);
    expect(
      find.textContaining('Uplaćena školarina se ne vraća.'),
      findsOneWidget,
    );
  });

  testWidgets('a course that is not enrolled hides Rang lista', (tester) async {
    final harness = _Harness();
    await harness.bootstrap();
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Rang lista'), findsNothing);
    expect(find.byType(Tooltip), findsNothing);
  });

  testWidgets('returning from a lecture refetches the lectures page', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      coursePaidUntil: '2027-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 1);

    await tester.ensureVisible(find.textContaining('Akordi I'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Akordi I'));
    await tester.pumpAndSettle();
    expect(find.text('Prijavljeno'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 2);
  });

  testWidgets(
    'inactive membership shows a disabled Upiši se under the membership banner',
    (tester) async {
      final harness = _Harness();
      await harness.bootstrap(paidUntil: DateTime.utc(2026, 9, 8));
      await tester.pumpWidget(harness.app());
      await tester.pumpAndSettle();

      expect(
        find.textContaining('Članarina je istekla 08.09.2026.'),
        findsOneWidget,
      );
      expect(
        find.textContaining('Obratite se školi za obnovu.'),
        findsOneWidget,
      );
      final button = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Upiši se'),
      );
      expect(button.onPressed, isNull);
    },
  );

  testWidgets('enrolled with no paidUntil shows the unpaid banner and Plati', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentStatus: 'Active', enrollmentId: 5);
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Školarina nije plaćena.'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Plati'), findsOneWidget);
    expect(find.textContaining('Školarina je istekla'), findsNothing);
  });

  testWidgets('enrolled with a past paidUntil shows the expired banner', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      coursePaidUntil: '2020-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Školarina je istekla 01.01.2020.'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Obnovi'), findsOneWidget);
  });

  testWidgets('enrolled with a future paidUntil shows Plaćeno do', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      coursePaidUntil: '2027-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Plaćeno do'), findsOneWidget);
    expect(find.text('01.01.2027.'), findsOneWidget);
    expect(find.text('Školarina nije plaćena.'), findsNothing);
  });

  testWidgets('a free enrolled course shows no tuition banner', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      isFree: true,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Školarina nije plaćena.'), findsNothing);
    expect(find.textContaining('Školarina je istekla'), findsNothing);
    expect(find.text('Plaćeno do'), findsNothing);
  });

  testWidgets('Historija uplata opens the paged payment history', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      coursePaidUntil: '2027-01-01T00:00:00Z',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.ensureVisible(
      find.widgetWithText(OutlinedButton, 'Historija uplata'),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(OutlinedButton, 'Historija uplata'));
    await tester.pumpAndSettle();

    expect(
      countRequests(
        harness.client,
        'GET /api/v1/student/enrollments/5/tuition/history',
      ),
      1,
    );
    expect(find.text('Vraćeno'), findsOneWidget);
    expect(find.textContaining('Povrat: 800.00 KM'), findsOneWidget);
    expect(
      find.textContaining('Pristup: 09.09.2026. – 09.10.2026.'),
      findsOneWidget,
    );
  });

  testWidgets('a request does not open tuition', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentId: 5);
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(FilledButton, 'Upiši se'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(find.text('Plaćanje školarine'), findsNothing);
    expect(find.text('Plati 800.00 KM'), findsNothing);
  });

  testWidgets('rejected shows the reason and an enabled Upiši se', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Rejected',
      enrollmentDecisionNote: 'Grupa za ovaj termin je popunjena.',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Odbijeno'), findsOneWidget);
    expect(
      find.text(
        'Zahtjev za upis je odbijen: Grupa za ovaj termin je popunjena.',
      ),
      findsOneWidget,
    );
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Upiši se'),
    );
    expect(button.onPressed, isNotNull);
  });

  testWidgets('completed enrollment hides Upiši se', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentStatus: 'Completed');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Položen'), findsOneWidget);
    expect(
      find.text('Kurs ste završili. Ponovni upis nije moguć.'),
      findsOneWidget,
    );
    expect(find.widgetWithText(FilledButton, 'Upiši se'), findsNothing);
  });

  testWidgets('completed course banner uses the success tone', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentStatus: 'Completed');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    final banner = tester.widget<StatusBanner>(
      find.ancestor(
        of: find.text('Kurs ste završili. Ponovni upis nije moguć.'),
        matching: find.byType(StatusBanner),
      ),
    );
    expect(banner.tone, StatusTone.success);
  });

  testWidgets('ended course hides the enroll action', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(hasEnded: true);
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Kurs je završen. Upis više nije moguć.'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Upiši se'), findsNothing);
  });

  testWidgets('Otkaži zahtjev on a pending request POSTs unenroll', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentStatus: 'Pending');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(OutlinedButton, 'Otkaži zahtjev'));
    await tester.pumpAndSettle();
    expect(find.text('Otkazivanje zahtjeva'), findsOneWidget);
    expect(
      find.text(
        'Želite li otkazati zahtjev za upis na kurs "Osnove teorije muzike"?',
      ),
      findsOneWidget,
    );

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      countRequests(harness.client, 'POST /api/v1/student/courses/2/unenroll'),
      1,
    );
    expect(countRequests(harness.client, 'GET /api/v1/student/courses/2'), 2);
  });

  testWidgets('returning from tuition with a null result reloads the course', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentStatus: 'Active', enrollmentId: 5);
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();
    expect(countRequests(harness.client, 'GET /api/v1/student/courses/2'), 1);

    await tester.ensureVisible(find.widgetWithText(FilledButton, 'Plati'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'Plati'));
    await tester.pumpAndSettle();
    expect(find.text('Plaćanje školarine'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();
    expect(countRequests(harness.client, 'GET /api/v1/student/courses/2'), 2);
  });

  testWidgets('locked content shows the reason instead of the lectures', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      lectureCount: 12,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Predavanja (12) su privremeno nedostupna. '
        'Obnovite školarinu da biste nastavili.',
      ),
      findsOneWidget,
    );
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 0);
    expect(find.textContaining('Akordi I'), findsNothing);
    expect(find.textContaining('Predavanja ·'), findsNothing);
  });

  testWidgets('locked content hides Rang lista', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      lectureCount: 12,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Rang lista'), findsNothing);
    expect(find.byType(Tooltip), findsNothing);
  });

  testWidgets('renewing the tuition unlocks the lectures after the reload', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      lectureCount: 12,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();
    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 0);

    harness.access.paidUntil = '2027-01-01T00:00:00Z';
    await tester.ensureVisible(find.widgetWithText(FilledButton, 'Plati'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'Plati'));
    await tester.pumpAndSettle();
    expect(find.text('Plaćanje školarine'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();

    expect(countRequests(harness.client, 'GET /api/v1/student/lectures'), 1);
    expect(find.textContaining('Akordi I'), findsOneWidget);
    expect(
      find.text(
        'Predavanja (12) su privremeno nedostupna. '
        'Obnovite školarinu da biste nastavili.',
      ),
      findsNothing,
    );
  });

  testWidgets('course lectures render as LectureTile rows with dividers', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      hasContentAccess: true,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.drag(find.byType(CustomScrollView), const Offset(0, -500));
    await tester.pumpAndSettle();

    expect(find.byType(LectureTile), findsOneWidget);
    expect(
      find.descendant(
        of: find.byType(LectureTile),
        matching: find.textContaining('Akordi I'),
      ),
      findsOneWidget,
    );
    expect(find.byType(Divider), findsWidgets);
  });

  testWidgets('Otkaži zahtjev uses the danger style', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(enrollmentStatus: 'Pending');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    final style = tester
        .widget<OutlinedButton>(
          find.widgetWithText(OutlinedButton, 'Otkaži zahtjev'),
        )
        .style;
    expect(style?.foregroundColor?.resolve({}), AppTheme.error);
  });

  testWidgets('course actions are full width', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      coursePaidUntil: '2027-01-01T00:00:00Z',
      hasContentAccess: true,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    final unenroll = tester.getSize(
      find.widgetWithText(OutlinedButton, 'Ispiši se'),
    );
    final ranking = tester.getSize(
      find.widgetWithText(OutlinedButton, 'Rang lista'),
    );
    final history = tester.getSize(
      find.widgetWithText(OutlinedButton, 'Historija uplata'),
    );
    expect(unenroll.width, ranking.width);
    expect(history.width, ranking.width);
    expect(ranking.width, greaterThan(300));
  });

  testWidgets('destructive action is the last action', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      coursePaidUntil: '2020-01-01T00:00:00Z',
      hasContentAccess: true,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    final destructive = tester
        .getRect(find.widgetWithText(OutlinedButton, 'Ispiši se'))
        .bottom;
    expect(
      destructive,
      greaterThan(
        tester.getRect(find.widgetWithText(FilledButton, 'Obnovi')).bottom,
      ),
    );
    expect(
      destructive,
      greaterThan(
        tester
            .getRect(find.widgetWithText(OutlinedButton, 'Rang lista'))
            .bottom,
      ),
    );
    expect(
      destructive,
      greaterThan(
        tester
            .getRect(find.widgetWithText(OutlinedButton, 'Historija uplata'))
            .bottom,
      ),
    );
  });

  testWidgets('locked content banner has no action button', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      lectureCount: 12,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Predavanja (12) su privremeno nedostupna. '
        'Obnovite školarinu da biste nastavili.',
      ),
      findsOneWidget,
    );
    expect(find.widgetWithText(TextButton, 'Obnovi školarinu'), findsNothing);
  });

  testWidgets('locked content shows the tuition button', (tester) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrollmentStatus: 'Active',
      enrollmentId: 5,
      coursePaidUntil: '2027-01-01T00:00:00Z',
      hasContentAccess: false,
      lectureCount: 12,
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Predavanja (12) su privremeno nedostupna. '
        'Obnovite školarinu da biste nastavili.',
      ),
      findsOneWidget,
    );
    expect(find.widgetWithText(FilledButton, 'Plati'), findsOneWidget);
  });
}
