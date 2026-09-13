import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/profile/profile_screen.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/widgets/async_state_view.dart';

import '../../helpers.dart';

class _ProfileTestClient extends http.BaseClient {
  int statusCode;
  final Map<String, dynamic> successBody;

  _ProfileTestClient({
    required this.statusCode,
    required this.successBody,
  });

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    if (path.endsWith('users/me')) {
      if (statusCode >= 400) {
        return http.StreamedResponse(
          Stream.value(utf8.encode('{"message":"Greška"}')),
          statusCode,
          headers: {'content-type': 'application/json'},
        );
      }
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode(successBody))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    if (path.endsWith('notifications/unread-count')) {
      return http.StreamedResponse(
        Stream.value(utf8.encode('{"unreadCount":0}')),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    if (path.endsWith('notifications')) {
      return http.StreamedResponse(
        Stream.value(utf8.encode('{"items":[],"totalCount":0}')),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets(
    'users/me returns 500 at boot shows AsyncStateView error with retry, retry succeeds shows content',
    (tester) async {
      final successBody = {
        'role': 'Student',
        'username': 'student',
        'email': 'student@enote.com',
        'profile': {
          'id': 7,
          'firstName': 'Student',
          'lastName': 'Enote',
        },
        'hasPicture': false,
      };

      final client = _ProfileTestClient(
        statusCode: 500,
        successBody: successBody,
      );

      final authState = AuthState(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        tokenReader: () => fakeJwt(),
        httpClient: client,
      );
      final apiClient = ApiClient(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        authState: authState,
        httpClient: client,
      );
      final session = SessionController(
        apiClient: apiClient,
        authState: authState,
        notifications: NotificationController(
          apiClient: apiClient,
          endpoint: 'student/notifications',
        ),
      );

      await session.bootstrap();

      await tester.pumpWidget(
        ChangeNotifierProvider<SessionController>.value(
          value: session,
          child: const MaterialApp(
            home: Scaffold(body: ProfileScreen()),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(AsyncStateView), findsOneWidget);
      expect(find.text('Pokušaj ponovo'), findsOneWidget);
      expect(find.text('Student Enote'), findsNothing);

      client.statusCode = 200;
      await tester.tap(find.text('Pokušaj ponovo'));
      await tester.pumpAndSettle();

      expect(find.byType(AsyncStateView), findsNothing);
      expect(find.text('Student Enote'), findsOneWidget);
    },
  );
}
