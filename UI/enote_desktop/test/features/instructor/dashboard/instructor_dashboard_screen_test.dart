import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/features/instructor/dashboard/instructor_dashboard_screen.dart';
import 'package:enote_desktop/features/instructor/enrollment/instructor_enrollment_provider.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';
import 'package:enote_desktop/shell/role_menu_entries.dart';
import 'package:enote_desktop/shell/shell_navigation.dart';
import 'package:enote_desktop/widgets/dashboard_stat_card.dart';

import '../../../helpers.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

http.Response _page(int totalCount) => jsonResponse({
  'items': [],
  'page': 1,
  'pageSize': 1,
  'totalCount': totalCount,
}, 200);

ScriptedClient _client({
  bool failStudents = false,
  int pendingEnrollments = 0,
}) {
  return ScriptedClient((request) {
    final url = request.url.toString();
    if (request.method == 'GET' && url.contains('instructor/courses')) {
      return _page(8);
    }
    if (request.method == 'GET' && url.contains('instructor/enrollments')) {
      return _page(pendingEnrollments);
    }
    if (request.method == 'GET' && url.contains('instructor/students')) {
      if (failStudents) {
        return jsonResponse({'message': 'Greška'}, 500);
      }
      return _page(14);
    }
    return _page(0);
  });
}

Widget _buildApp({
  required ScriptedClient httpClient,
  required void Function(String label) openEntry,
}) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(username: 'instructor', role: 'Instructor'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: httpClient,
  );

  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<CourseProvider>(
        create: (_) => CourseProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<InstructorStudentProvider>(
        create: (_) => InstructorStudentProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<InstructorEnrollmentProvider>(
        create: (_) => InstructorEnrollmentProvider(apiClient: apiClient),
      ),
    ],
    child: MaterialApp(
      home: ShellNavigation(
        openEntry: openEntry,
        child: const Scaffold(body: InstructorDashboardScreen()),
      ),
    ),
  );
}

void main() {
  testWidgets('shows my courses and my students counts', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    expect(find.text('8'), findsOneWidget);
    expect(find.text('14'), findsOneWidget);
    expect(find.text('–'), findsNothing);
  });

  testWidgets('one failed request shows a dash and keeps the other cards', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(failStudents: true), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    expect(find.text('–'), findsOneWidget);
    expect(find.text('8'), findsOneWidget);
  });

  testWidgets('shows Zahtjevi na čekanju first with the pending count', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(pendingEnrollments: 3), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    final cards = tester
        .widgetList<DashboardStatCard>(find.byType(DashboardStatCard))
        .toList();
    expect(cards.first.label, 'Zahtjevi na čekanju');
    expect(cards.first.icon, Icons.assignment_outlined);
    expect(cards.first.tone, StatusTone.warning);
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets('tapping the Zahtjevi card opens the Zahtjevi entry', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    String? opened;
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (label) => opened = label),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Zahtjevi na čekanju'));
    await tester.pump();

    expect(opened, kRequestsEntryLabel);
  });

  testWidgets('the other instructor cards are not tappable', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    String? opened;
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (label) => opened = label),
    );
    await tester.pumpAndSettle();

    final cards = tester
        .widgetList<DashboardStatCard>(find.byType(DashboardStatCard))
        .where((card) => card.label != 'Zahtjevi na čekanju');
    for (final card in cards) {
      expect(card.onTap, isNull);
    }

    await tester.tap(find.text('Moji kursevi'));
    await tester.pump();

    expect(opened, isNull);
  });
}
