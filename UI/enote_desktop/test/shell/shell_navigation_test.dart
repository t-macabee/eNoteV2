import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/course/admin_course_list_screen.dart';
import 'package:enote_desktop/features/admin/dashboard/admin_dashboard_screen.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/shell_navigation.dart';

import '../helpers.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

ScriptedClient _client() {
  return ScriptedClient((request) {
    final url = request.url.toString();
    if (url.contains('notifications/unread-count')) {
      return jsonResponse({'unreadCount': 0}, 200);
    }
    if (url.contains('notifications')) {
      return jsonResponse(const {'items': [], 'unreadCount': 0}, 200);
    }
    return jsonResponse(const {
      'items': [],
      'page': 1,
      'pageSize': 1,
      'totalCount': 0,
    }, 200);
  });
}

Widget _buildApp() {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(username: 'admin', role: 'Administrator'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: _client(),
  );

  return MyApp(authState: authState, apiClient: apiClient);
}

void main() {
  testWidgets('openEntry selects the entry with the given label', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    expect(find.byType(AdminDashboardScreen), findsOneWidget);

    tester
        .widget<ShellNavigation>(find.byType(ShellNavigation))
        .openEntry('Kursevi');
    await tester.pumpAndSettle();

    expect(find.byType(AdminCourseListScreen), findsOneWidget);
    expect(find.byType(AdminDashboardScreen), findsNothing);
  });

  testWidgets('openEntry ignores an unknown label', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    tester
        .widget<ShellNavigation>(find.byType(ShellNavigation))
        .openEntry('Nepoznata opcija');
    await tester.pumpAndSettle();

    expect(find.byType(AdminDashboardScreen), findsOneWidget);
  });
}
