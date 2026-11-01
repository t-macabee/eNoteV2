import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/dashboard/admin_dashboard_screen.dart';
import 'package:enote_desktop/features/instructor/dashboard/instructor_dashboard_screen.dart';
import 'package:enote_desktop/features/store_employee/dashboard/employee_dashboard_screen.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/role_menu.dart';

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
    if (request.method == 'GET' && url.contains('shop/store')) {
      return jsonResponse(const {
        'id': 1,
        'storeName': 'Muzička Prodavnica',
        'businessHours': '08:00 - 16:00',
      }, 200);
    }
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

Widget _buildApp(String role) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(username: 'user', role: role),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: _client(),
  );

  return MyApp(authState: authState, apiClient: apiClient);
}

Finder _menuEntry(String label) =>
    find.descendant(of: find.byType(RoleMenu), matching: find.text(label));

Finder _backButton() => find.ancestor(
  of: find.byType(BackButtonIcon),
  matching: find.byType(IconButton),
);

Future<void> _expectHomeAndBackBehaviour(
  WidgetTester tester, {
  required String role,
  required Type dashboard,
  required String otherEntry,
}) async {
  _useDesktopView(tester);
  await tester.pumpWidget(_buildApp(role));
  await tester.pumpAndSettle();

  expect(find.text('Početna'), findsNWidgets(2));
  expect(find.byType(dashboard), findsOneWidget);
  expect(tester.widget<IconButton>(_backButton()).onPressed, isNull);

  await tester.tap(_menuEntry(otherEntry));
  await tester.pumpAndSettle();

  expect(tester.widget<IconButton>(_backButton()).onPressed, isNotNull);
}

void main() {
  testWidgets('administrator lands on Početna after login', (
    WidgetTester tester,
  ) async {
    await _expectHomeAndBackBehaviour(
      tester,
      role: 'Administrator',
      dashboard: AdminDashboardScreen,
      otherEntry: 'Korisnici',
    );
  });

  testWidgets('instructor lands on Početna after login', (
    WidgetTester tester,
  ) async {
    await _expectHomeAndBackBehaviour(
      tester,
      role: 'Instructor',
      dashboard: InstructorDashboardScreen,
      otherEntry: 'Kursevi',
    );
  });

  testWidgets('store employee lands on Početna after login', (
    WidgetTester tester,
  ) async {
    await _expectHomeAndBackBehaviour(
      tester,
      role: 'StoreEmployee',
      dashboard: EmployeeDashboardScreen,
      otherEntry: 'Zaposlenici',
    );
  });
}
