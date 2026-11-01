import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/course/admin_course_provider.dart';
import 'package:enote_desktop/features/admin/dashboard/admin_dashboard_screen.dart';
import 'package:enote_desktop/features/admin/event/event_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/features/admin/users/admin_user_provider.dart';
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

ScriptedClient _client({bool failCourses = false}) {
  return ScriptedClient((request) {
    final url = request.url.toString();
    if (request.method == 'GET' && url.contains('admin/users')) {
      return _page(12);
    }
    if (request.method == 'GET' && url.contains('admin/music-stores')) {
      return _page(3);
    }
    if (request.method == 'GET' && url.contains('admin/courses')) {
      if (failCourses) {
        return jsonResponse({'message': 'Greška'}, 500);
      }
      return _page(7);
    }
    if (request.method == 'GET' && url.contains('admin/events')) {
      return _page(5);
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
    tokenReader: () => fakeJwt(username: 'admin', role: 'Administrator'),
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
      ChangeNotifierProvider<AdminUserProvider>(
        create: (_) => AdminUserProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<MusicStoreProvider>(
        create: (_) => MusicStoreProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<AdminCourseProvider>(
        create: (_) => AdminCourseProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<EventProvider>(
        create: (_) => EventProvider(apiClient: apiClient),
      ),
    ],
    child: MaterialApp(
      home: ShellNavigation(
        openEntry: openEntry,
        child: const Scaffold(body: AdminDashboardScreen()),
      ),
    ),
  );
}

Finder _card(String label) => find.byWidgetPredicate(
  (widget) => widget is DashboardStatCard && widget.label == label,
);

void main() {
  testWidgets('shows the four counts from totalCount', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    expect(find.text('12'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
    expect(find.text('7'), findsOneWidget);
    expect(find.text('5'), findsOneWidget);
    expect(find.text('–'), findsNothing);
  });

  testWidgets('one failed request shows a dash and keeps the other cards', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(failCourses: true), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    expect(find.text('–'), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
    expect(find.text('5'), findsOneWidget);
  });

  testWidgets('admin cards are not tappable', (WidgetTester tester) async {
    _useDesktopView(tester);
    String? opened;
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (label) => opened = label),
    );
    await tester.pumpAndSettle();

    for (final card in tester.widgetList<DashboardStatCard>(
      find.byType(DashboardStatCard),
    )) {
      expect(card.onTap, isNull);
    }

    await tester.tap(
      find.descendant(of: _card('Kursevi'), matching: find.text('Kursevi')),
    );
    await tester.pump();

    expect(opened, isNull);
  });
}
