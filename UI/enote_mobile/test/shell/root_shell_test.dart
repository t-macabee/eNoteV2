import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/rentals/payments/rental_payment_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/realtime/notification_hub_client.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/shell/root_shell.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../helpers.dart';

class _FakeHub extends NotificationHubClient {
  int startCalls = 0;
  int stopCalls = 0;

  _FakeHub()
    : super(
        hubUrl: 'http://localhost/hubs/notifications',
        tokenProvider: () async => 'token',
      );

  @override
  Future<void> start() async {
    startCalls++;
  }

  @override
  Future<void> stop() async {
    stopCalls++;
  }
}

class _Harness {
  late final ScriptedClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final NotificationController notifications;
  late final SessionController session;
  late final _FakeHub hub;

  _Harness() {
    client = ScriptedClient((request) {
      final path = request.url.path;
      late Object body;
      if (request.method == 'GET' && path == '/api/v1/users/me') {
        body = meJson();
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/notifications/unread-count') {
        body = {'unreadCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/notifications') {
        body = {'items': [], 'totalCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/instruments') {
        body = {'items': [], 'totalCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/rentals') {
        body = {'items': [], 'totalCount': 0};
      } else {
        body = {'message': 'OK'};
      }
      return jsonResponse(body, 200);
    });
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
    notifications = NotificationController(
      apiClient: apiClient,
      endpoint: 'student/notifications',
    );
    hub = _FakeHub();
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: notifications,
    );
  }

  Widget shellApp() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<NotificationController>.value(
          value: notifications,
        ),
        Provider<NotificationHubClient>.value(value: hub),
        ChangeNotifierProvider<SessionController>.value(value: session),
        ChangeNotifierProvider<InstrumentProvider>(
          create: (_) => InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
        Provider<RentalPaymentProvider>(
          create: (_) => RentalPaymentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: RootShell(key: RootShell.shellKey),
      ),
    );
  }
}

void main() {
  testWidgets('disposing the shell stops the hub', (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.shellApp());
    await tester.pumpAndSettle();

    expect(harness.hub.startCalls, 1);

    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pumpAndSettle();

    expect(harness.hub.stopCalls, 1);
  });
}
