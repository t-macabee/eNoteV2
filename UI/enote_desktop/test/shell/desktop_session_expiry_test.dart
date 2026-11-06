import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/reference_data/reference_data_dialog.dart';
import 'package:enote_desktop/main.dart';
import 'package:enote_desktop/shell/login_screen.dart';
import 'package:enote_desktop/shell/master_screen.dart';

import '../helpers.dart';

void main() {
  testWidgets(
    'fake 401 on non-auth path with a dialog open pops to LoginScreen with no dialog',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      var return401 = false;
      final mockHttp = ScriptedClient((request) {
        if (return401 && !request.url.path.contains('/auth/')) {
          return jsonResponse({'message': 'Unauthorized'}, 401);
        }
        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 10,
          'totalCount': 0,
        }, 200);
      });
      final navigatorKey = GlobalKey<NavigatorState>();
      late AuthState authState;
      final sessionHttp = SessionHttpClient(
        inner: mockHttp,
        timeout: const Duration(seconds: 20),
        onUnauthorized: () => handleSessionExpired(navigatorKey, authState),
      );
      authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: sessionHttp,
        tokenReader: () => fakeJwt(role: 'Administrator'),
      );
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: sessionHttp,
      );

      await tester.pumpWidget(
        MyApp(
          authState: authState,
          apiClient: apiClient,
          navigatorKey: navigatorKey,
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(MasterScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);

      await tester.tap(find.text('Referentni podaci'));
      await tester.pumpAndSettle();

      expect(find.byType(ReferenceDataDialog), findsOneWidget);

      return401 = true;

      try {
        await apiClient.get('courses');
      } catch (_) {}

      await tester.pumpAndSettle();

      expect(find.byType(LoginScreen), findsOneWidget);
      expect(find.byType(ReferenceDataDialog), findsNothing);
      expect(find.byType(Dialog), findsNothing);
      expect(authState.isAuthenticated, isFalse);
      expect(
        find.text('Vaša sesija je istekla. Prijavite se ponovo.'),
        findsOneWidget,
      );
    },
  );

  testWidgets('a plain logout shows the login with no session text', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final harness = _harness(role: 'Administrator');

    await tester.pumpWidget(_app(harness));
    await tester.pumpAndSettle();
    await harness.authState.logout();
    await tester.pumpAndSettle();

    expect(find.byType(LoginScreen), findsOneWidget);
    expect(
      find.text('Vaša sesija je istekla. Prijavite se ponovo.'),
      findsNothing,
    );
  });

  testWidgets('the session text does not come back after the next login', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final harness = _harness(role: 'Administrator');

    await tester.pumpWidget(_app(harness));
    await tester.pumpAndSettle();
    await harness.authState.expireSession();
    await tester.pumpAndSettle();
    expect(
      find.text('Vaša sesija je istekla. Prijavite se ponovo.'),
      findsOneWidget,
    );

    await harness.authState.login('admin', 'p');
    await tester.pumpAndSettle();
    await harness.authState.logout();
    await tester.pumpAndSettle();

    expect(find.byType(LoginScreen), findsOneWidget);
    expect(
      find.text('Vaša sesija je istekla. Prijavite se ponovo.'),
      findsNothing,
    );
  });

  testWidgets('a student sees the blocked screen and can log out', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final harness = _harness(role: 'Student');

    await tester.pumpWidget(_app(harness));
    await tester.pumpAndSettle();

    expect(find.byType(MasterScreen), findsNothing);
    expect(
      find.text(
        'Ova aplikacija je namijenjena administratorima, instruktorima i '
        'uposlenicima prodavnice. Prijavite se na mobilnu aplikaciju.',
      ),
      findsOneWidget,
    );
    expect(find.text('Molimo odaberite opciju iz izbornika.'), findsNothing);

    await tester.tap(find.text('Odjavi se'));
    await tester.pumpAndSettle();

    expect(find.byType(LoginScreen), findsOneWidget);
    expect(harness.authState.isAuthenticated, isFalse);
    expect(harness.logoutRequests, 1);
  });

  for (final role in ['Administrator', 'Instructor', 'StoreEmployee']) {
    testWidgets('$role gets the shell, not the blocked screen', (
      WidgetTester tester,
    ) async {
      tester.view.physicalSize = const Size(1400, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final harness = _harness(role: role);

      await tester.pumpWidget(_app(harness));
      await tester.pumpAndSettle();

      expect(find.byType(MasterScreen), findsOneWidget);
      expect(find.text('Odjavi se'), findsNothing);
    });
  }

  testWidgets('the next user does not hold the notifications of the last user', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final harness = _harness(role: 'Administrator', unreadCount: 7);

    await tester.pumpWidget(_app(harness));
    await tester.pumpAndSettle();
    final controller = tester
        .element(find.byType(MasterScreen))
        .read<NotificationController>();
    expect(controller.notifications, hasLength(1));
    expect(controller.unreadCount, 7);

    await harness.authState.logout();
    await tester.pumpAndSettle();
    expect(controller.notifications, isEmpty);
    expect(controller.unreadCount, 0);

    harness.failNotifications = true;
    await harness.authState.login('admin', 'p');
    await tester.pumpAndSettle();

    expect(controller.notifications, isEmpty);
    expect(controller.unreadCount, 0);

    await harness.authState.logout();
    await tester.pumpAndSettle();
  });
}

class _Harness {
  late final AuthState authState;
  late final ApiClient apiClient;
  final navigatorKey = GlobalKey<NavigatorState>();
  bool failNotifications = false;
  int logoutRequests = 0;
}

_Harness _harness({required String role, int unreadCount = 0}) {
  final harness = _Harness();
  final token = fakeJwt(role: role);
  final client = ScriptedClient((request) {
    final path = request.url.path;
    if (path.endsWith('auth/login')) {
      return jsonResponse({
        'userId': 1,
        'username': 'admin',
        'roles': [role],
        'token': token,
      }, 200);
    }
    if (path.endsWith('auth/logout')) {
      harness.logoutRequests++;
      return jsonResponse(const {}, 200);
    }
    if (path.endsWith('notifications/unread-count')) {
      return harness.failNotifications
          ? jsonResponse(const {'message': 'x'}, 500)
          : jsonResponse({'unreadCount': unreadCount}, 200);
    }
    if (path.endsWith('notifications')) {
      return harness.failNotifications
          ? jsonResponse(const {'message': 'x'}, 500)
          : jsonResponse({
              'items': [
                {
                  'id': 1,
                  'title': 'Obavijest',
                  'body': 'b',
                  'isRead': false,
                  'createdAt': '2026-01-01T00:00:00Z',
                },
              ],
              'totalCount': 1,
            }, 200);
    }
    return jsonResponse(const {
      'items': [],
      'page': 1,
      'pageSize': 10,
      'totalCount': 0,
    }, 200);
  });
  harness.authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: client,
    tokenReader: () => token,
    tokenRevoker: () => harness.apiClient.post('auth/logout'),
  );
  harness.apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: harness.authState,
    httpClient: client,
  );
  return harness;
}

MyApp _app(_Harness harness) => MyApp(
  authState: harness.authState,
  apiClient: harness.apiClient,
  navigatorKey: harness.navigatorKey,
);
