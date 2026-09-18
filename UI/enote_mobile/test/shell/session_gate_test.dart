import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/auth/auth_provider.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/rentals/payments/rental_payment_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/realtime/notification_hub_client.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/session_gate.dart';

import '../helpers.dart';

Map<String, dynamic> _gateBody(http.Request request) {
  switch (request.url.path) {
    case '/api/v1/users/me':
      return {
        'role': 'Student',
        'username': 'student',
        'email': 'student@enote.com',
        'profile': {'id': 7},
        'hasPicture': false,
      };
    case '/api/v1/student/notifications/unread-count':
      return {'unreadCount': 0};
    case '/api/v1/student/notifications':
      return {
        'items': [],
        'totalCount': 0,
      };
    default:
      return {'message': 'OK'};
  }
}

Widget _gate({String? token}) {
  final stub = ScriptedClient(
    (request) => jsonResponse(jsonEncode(_gateBody(request)), 200),
  );
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => token,
    tokenWriter: (_) {},
    httpClient: stub,
  );
  final apiClient = ApiClient(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    authState: authState,
    httpClient: stub,
  );
  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<NotificationController>(
        create: (_) => NotificationController(
          apiClient: apiClient,
          endpoint: 'student/notifications',
        ),
        lazy: false,
      ),
      Provider<NotificationHubClient>(
        create: (_) => NotificationHubClient(
          hubUrl: NotificationHubClient.deriveHubUrl(
            'http://10.0.2.2:5059/api/v1/',
          ),
          tokenProvider: () async => 'token',
        ),
        dispose: (_, client) => client.dispose(),
      ),
      ChangeNotifierProvider<SessionController>(
        create: (context) => SessionController(
          apiClient: apiClient,
          authState: authState,
          notifications: context.read<NotificationController>(),
        ),
        lazy: false,
      ),
      Provider<AuthProvider>(
        create: (_) => AuthProvider(apiClient: apiClient),
      ),
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
    child: const MaterialApp(home: SessionGate()),
  );
}

void main() {
  testWidgets('no token shows the login screen', (tester) async {
    await tester.pumpWidget(_gate());
    await tester.pumpAndSettle();
    expect(find.text('Prijava'), findsWidgets);
    expect(find.text('Korisničko ime'), findsOneWidget);
  });

  testWidgets('a student token shows the root shell', (tester) async {
    await tester.pumpWidget(_gate(token: fakeJwt(role: 'Student')));
    await tester.pumpAndSettle();
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(find.text('Instrumenti'), findsWidgets);
  });

  testWidgets('a non-student token shows role blocked with logout', (
    tester,
  ) async {
    await tester.pumpWidget(_gate(token: fakeJwt(role: 'Instructor')));
    await tester.pumpAndSettle();
    expect(
      find.text(
        'Ova aplikacija je namijenjena studentima. Prijavite se na desktop aplikaciju.',
      ),
      findsOneWidget,
    );
    await tester.tap(find.text('Odjavi se'));
    await tester.pumpAndSettle();
    expect(find.text('Korisničko ime'), findsOneWidget);
  });
}
