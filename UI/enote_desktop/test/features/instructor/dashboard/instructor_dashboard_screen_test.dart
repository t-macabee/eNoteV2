import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/features/instructor/dashboard/instructor_dashboard_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';
import 'package:enote_desktop/shell/shell_navigation.dart';

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

ScriptedClient _client({bool failStudents = false}) {
  return ScriptedClient((request) {
    final url = request.url.toString();
    if (request.method == 'GET' && url.contains('instructor/courses')) {
      return _page(8);
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
}
