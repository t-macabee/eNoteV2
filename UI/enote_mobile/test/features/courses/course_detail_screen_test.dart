import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/courses/course_detail_screen.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

Map<String, dynamic> _courseJson({required bool enrolled}) => {
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
  'isEnrolled': enrolled,
};

Map<String, dynamic> _meJson({String paidUntil = '2027-09-09T00:00:00'}) => {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {'id': 7, 'membershipPaidUntil': paidUntil},
  'hasPicture': false,
};

const _lectureJson = {
  'id': 11,
  'name': 'Akordi I',
  'location': 'Sala 2',
  'lectureType': 'Theoretical',
  'lectureStatus': 1,
  'isCancelled': false,
  'lectureTime': '2026-09-14T18:00:00',
  'duration': 60,
  'capacity': 20,
  'attendeeCount': 5,
  'myAttendanceStatus': 1,
};

const _lecturesPage = {
  'items': [_lectureJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

class _CourseDetailStubClient extends http.BaseClient {
  final List<String> calls = [];
  bool enrolled;
  final String paidUntil;

  _CourseDetailStubClient({required this.enrolled, required this.paidUntil});

  int count(String marker) => calls
      .where((c) => c == marker || c.startsWith('$marker?'))
      .length;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    calls.add('${request.method} $path?${request.url.query}');
    Object body;
    switch ('${request.method} $path') {
      case 'GET /api/v1/users/me':
        body = _meJson(paidUntil: paidUntil);
      case 'GET /api/v1/student/notifications/unread-count':
        body = {'unreadCount': 0};
      case 'GET /api/v1/student/notifications':
        body = {'items': []};
      case 'GET /api/v1/student/courses/2':
        body = _courseJson(enrolled: enrolled);
      case 'GET /api/v1/student/lectures':
        body = _lecturesPage;
      case 'POST /api/v1/student/courses/2/enroll':
        enrolled = true;
        body = {'message': 'OK'};
      case 'POST /api/v1/student/courses/2/unenroll':
        enrolled = false;
        body = {'message': 'OK'};
      default:
        body = {'message': 'OK'};
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _Harness {
  late final _CourseDetailStubClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final SessionController session;

  Future<void> bootstrap({
    bool enrolled = false,
    String paidUntil = '2027-09-09T00:00:00',
  }) async {
    client = _CourseDetailStubClient(
      enrolled: enrolled,
      paidUntil: paidUntil,
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
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: const CourseDetailScreen(courseId: 2),
      ),
    );
  }
}

void main() {
  testWidgets(
    'not enrolled shows the empty-state copy and no further lectures request',
    (tester) async {
      final harness = _Harness();
      await harness.bootstrap(enrolled: false);
      await tester.pumpWidget(harness.app());
      await tester.pumpAndSettle();

      expect(
        find.text('Upišite se da vidite predavanja ovog kursa.'),
        findsOneWidget,
      );
      expect(find.textContaining('Akordi I'), findsNothing);
      // The initial load (02 D7) fires exactly one lectures request.
      expect(harness.client.count('GET /api/v1/student/lectures'), 1);

      await tester.pump(const Duration(seconds: 1));
      expect(harness.client.count('GET /api/v1/student/lectures'), 1);
    },
  );

  testWidgets('enrolled renders the master card and lecture rows', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrolled: true);
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

  testWidgets('tap Upi\u0161i se confirms and refetches both halves', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrolled: false);
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
      harness.client.count('POST /api/v1/student/courses/2/enroll'),
      1,
    );
    expect(harness.client.count('GET /api/v1/student/courses/2'), 2);
    expect(harness.client.count('GET /api/v1/student/lectures'), 2);
    expect(
      find.text('Uspješno ste upisani na kurs Osnove teorije muzike.'),
      findsOneWidget,
    );
    expect(find.text('Upisan'), findsOneWidget);
    expect(find.textContaining('Akordi I'), findsOneWidget);
  });

  testWidgets('Ispiši se refetches the master half only, no snackbar', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrolled: true);
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(OutlinedButton, 'Ispiši se'));
    await tester.pumpAndSettle();
    expect(find.text('Ispis sa kursa'), findsOneWidget);
    expect(
      find.text('Želite li se ispisati sa kursa "Osnove teorije muzike"?'),
      findsOneWidget,
    );

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      harness.client.count('POST /api/v1/student/courses/2/unenroll'),
      1,
    );
    expect(harness.client.count('GET /api/v1/student/courses/2'), 2);
    // No further lectures request after the initial load (03 §5.3 step 3).
    expect(harness.client.count('GET /api/v1/student/lectures'), 1);
    expect(
      find.text('Upišite se da vidite predavanja ovog kursa.'),
      findsOneWidget,
    );
    expect(
      find.text('Uspješno ste upisani na kurs Osnove teorije muzike.'),
      findsNothing,
    );
  });

  testWidgets('returning from a lecture refetches the lectures page', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(enrolled: true);
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();
    expect(harness.client.count('GET /api/v1/student/lectures'), 1);

    await tester.tap(find.textContaining('Akordi I'));
    await tester.pumpAndSettle();
    // The router case pushes the real S18 through the scoped providers.
    expect(find.text('Prijavljeno'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();
    expect(harness.client.count('GET /api/v1/student/lectures'), 2);
  });

  testWidgets('inactive membership disables the button under the banner', (
    tester,
  ) async {
    final harness = _Harness();
    await harness.bootstrap(
      enrolled: false,
      paidUntil: '2026-09-08T00:00:00',
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(
      find.textContaining('Članarina je istekla 08.09.2026.'),
      findsOneWidget,
    );
    expect(find.textContaining('Obratite se školi za obnovu.'), findsOneWidget);
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Upiši se'),
    );
    expect(button.onPressed, isNull);
  });
}
