import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/profile/profile_screen.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/widgets/async_state_view.dart';

import '../../helpers.dart';

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

      var statusCode = 500;
      final client = ScriptedClient((request) {
        final path = request.url.path;
        if (path.endsWith('users/me')) {
          if (statusCode >= 400) {
            return jsonResponse('{"message":"Greška"}', statusCode);
          }
          return jsonResponse(jsonEncode(successBody), 200);
        }
        if (path.endsWith('notifications/unread-count')) {
          return jsonResponse('{"unreadCount":0}', 200);
        }
        if (path.endsWith('notifications')) {
          return jsonResponse('{"items":[],"totalCount":0}', 200);
        }
        return jsonResponse('{}', 200);
      });

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

      statusCode = 200;
      await tester.tap(find.text('Pokušaj ponovo'));
      await tester.pumpAndSettle();

      expect(find.byType(AsyncStateView), findsNothing);
      expect(find.text('Student Enote'), findsOneWidget);
    },
  );
}
