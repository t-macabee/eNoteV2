import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/enrollment/instructor_enrollment_list_screen.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/role_menu.dart';
import 'package:enote_desktop/shell/role_menu_entries.dart';

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
    tokenReader: () => fakeJwt(username: 'instructor', role: 'Instructor'),
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

void main() {
  testWidgets('instructor menu shows Zahtjevi after Kursevi', (tester) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    final kursevi = tester.getTopLeft(_menuEntry('Kursevi'));
    final zahtjevi = tester.getTopLeft(_menuEntry(kRequestsEntryLabel));

    expect(zahtjevi.dy, greaterThan(kursevi.dy));
    expect(
      find.descendant(
        of: find.byType(RoleMenu),
        matching: find.byIcon(Icons.assignment_outlined),
      ),
      findsOneWidget,
    );
  });

  testWidgets('tapping Zahtjevi opens the instructor enrollment queue', (
    tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.tap(_menuEntry(kRequestsEntryLabel));
    await tester.pumpAndSettle();

    expect(find.byType(InstructorEnrollmentListScreen), findsOneWidget);
  });
}
