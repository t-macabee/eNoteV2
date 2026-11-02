import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/assignments/assignment_provider.dart';
import 'package:enote_mobile/features/assignments/assignment_tile.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';
import 'package:enote_mobile/features/home/home_provider.dart';
import 'package:enote_mobile/features/home/home_screen.dart';
import 'package:enote_mobile/features/lectures/lecture_detail_screen.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';
import 'package:enote_mobile/features/lectures/lecture_tile.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';
const _homePath = 'GET /api/v1/student/home';

final _futureTime = DateTime.now()
    .add(const Duration(days: 365))
    .toIso8601String();

Map<String, dynamic> _lecture() => {
  'id': 11,
  'name': 'Akordi I',
  'location': 'Sala 2',
  'lectureType': 'Theoretical',
  'lectureStatus': 'Scheduled',
  'isCancelled': false,
  'lectureTime': _futureTime,
  'duration': 60,
  'capacity': 20,
  'attendeeCount': 5,
  'myAttendanceStatus': 'Pending',
};

Map<String, dynamic> _assignment(int id) => {
  'id': id,
  'lectureId': 11,
  'title': 'Zadatak $id',
  'description': 'Opis',
  'dueAt': _futureTime,
};

Map<String, dynamic> _course({DateTime? paidUntil}) => {
  'id': 3,
  'name': 'Gitara 2',
  'enrollmentStatus': 'Active',
  'isFree': false,
  'price': 40,
  'paidUntil': ?paidUntil?.toIso8601String(),
};

Map<String, dynamic> _home({
  Map<String, dynamic>? nextLecture,
  List<Map<String, dynamic>> assignments = const [],
  List<Map<String, dynamic>> courses = const [],
  bool hasUnpaidDebt = false,
  int? debtRentalId,
}) => {
  'nextLecture': nextLecture,
  'openAssignments': assignments,
  'currentRentals': <Map<String, dynamic>>[],
  'tuitionDueCourses': courses,
  'debt': {'hasUnpaidDebt': hasUnpaidDebt, 'rentalId': debtRentalId},
};

Future<ScriptedClient> _pumpHome(
  WidgetTester tester, {
  required Map<String, dynamic> home,
  DateTime? paidUntil,
}) async {
  setPhoneSurface(tester);
  final client = ScriptedClient((request) {
    final body = switch (request.url.path) {
      '/api/v1/users/me' => meJson(paidUntil: paidUntil),
      '/api/v1/student/home' => home,
      '/api/v1/student/lectures/11' => _lecture(),
      '/api/v1/student/notifications/unread-count' => {'unreadCount': 0},
      '/api/v1/student/notifications' => {'items': [], 'totalCount': 0},
      _ => {'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0},
    };
    return jsonResponse(jsonEncode(body), 200);
  });
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: _baseUrl,
    authState: authState,
    httpClient: client,
  );
  final session = SessionController(
    apiClient: apiClient,
    authState: authState,
    notifications: NotificationController(
      apiClient: apiClient,
      endpoint: 'student/notifications',
    ),
  );
  await session.bootstrap();

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<SessionController>.value(value: session),
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<CourseProvider>(
          create: (_) => CourseProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<AssignmentProvider>(
          create: (_) => AssignmentProvider(apiClient: apiClient),
        ),
        Provider<HomeProvider>(
          create: (_) => HomeProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: mobileTheme,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: const Scaffold(body: HomeScreen()),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('shows the next lecture tile', (tester) async {
    await _pumpHome(tester, home: _home(nextLecture: _lecture()));

    expect(find.byType(LectureTile), findsOneWidget);
    expect(find.text('Akordi I'), findsOneWidget);
  });

  testWidgets('shows Nema zakazanih predavanja. without a lecture', (
    tester,
  ) async {
    await _pumpHome(tester, home: _home());

    expect(find.text('Nema zakazanih predavanja.'), findsOneWidget);
  });

  testWidgets('shows the open assignments from the server', (tester) async {
    await _pumpHome(
      tester,
      home: _home(
        assignments: [for (var id = 1; id <= 3; id++) _assignment(id)],
      ),
    );

    expect(find.byType(AssignmentTile), findsNWidgets(3));
  });

  testWidgets('shows the debt banner with Plati sada', (tester) async {
    await _pumpHome(tester, home: _home(hasUnpaidDebt: true, debtRentalId: 5));

    expect(find.textContaining('neizmireno dugovanje'), findsOneWidget);
    expect(find.text('Plati sada'), findsOneWidget);
  });

  testWidgets('shows the membership banner when membership is not active', (
    tester,
  ) async {
    await _pumpHome(tester, home: _home(), paidUntil: DateTime.utc(2020, 1, 1));

    expect(find.textContaining('Članarina je istekla'), findsOneWidget);
  });

  testWidgets('hides the Školarina section when nothing is due', (
    tester,
  ) async {
    await _pumpHome(tester, home: _home());

    expect(find.text('ŠKOLARINA'), findsNothing);
  });

  testWidgets('shows the Školarina section with the unpaid tuition text', (
    tester,
  ) async {
    await _pumpHome(tester, home: _home(courses: [_course()]));

    expect(find.text('ŠKOLARINA'), findsOneWidget);
    expect(find.text('Školarina nije plaćena.'), findsOneWidget);
  });

  testWidgets('tapping the lecture tile opens the lecture detail', (
    tester,
  ) async {
    await _pumpHome(tester, home: _home(nextLecture: _lecture()));

    await tester.tap(find.byType(LectureTile));
    await tester.pumpAndSettle();

    expect(find.byType(LectureDetailScreen), findsOneWidget);
  });

  testWidgets('reloads after RentalProvider notifies', (tester) async {
    final client = await _pumpHome(tester, home: _home());
    expect(countRequests(client, _homePath), 1);

    tester
        .element(find.byType(HomeScreen))
        .read<RentalProvider>()
        .notifyListeners();
    await tester.pumpAndSettle();

    expect(countRequests(client, _homePath), 2);
  });

  testWidgets('pull to refresh reloads', (tester) async {
    final client = await _pumpHome(tester, home: _home());
    expect(countRequests(client, _homePath), 1);

    await tester.drag(find.byType(ListView), const Offset(0, 300));
    await tester.pumpAndSettle();

    expect(countRequests(client, _homePath), 2);
  });
}
