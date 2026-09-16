import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
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

Map<String, dynamic> _meJson() => {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {
    'id': 7,
    'firstName': 'Student',
    'lastName': 'Enote',
    'dateOfBirth': '2001-05-12T00:00:00',
    'membershipPaidUntil': DateTime.utc(2027, 9, 9).toIso8601String(),
  },
  'hasPicture': false,
};

class _ShellStubClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    late Object body;
    if (request.method == 'GET' && path == '/api/v1/users/me') {
      body = _meJson();
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
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

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
  late final _ShellStubClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final NotificationController notifications;
  late final SessionController session;
  late final _FakeHub hub;

  _Harness() {
    client = _ShellStubClient();
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
