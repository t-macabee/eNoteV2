import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/profile/profile_provider.dart';
import 'package:enote_mobile/features/profile/profile_screen.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/widgets/async_state_view.dart';

import '../../helpers.dart';

const _meBody = {
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

class _Harness {
  int meStatus;
  late final ScriptedClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final SessionController session;

  _Harness({
    this.meStatus = 200,
    int deleteStatus = 204,
    Map<String, dynamic> deleteBody = const {},
  }) {
    client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.endsWith('users/me') && request.method == 'DELETE') {
        return jsonResponse(deleteBody, deleteStatus);
      }
      if (path.endsWith('users/me')) {
        return meStatus >= 400
            ? jsonResponse('{"message":"Greška"}', meStatus)
            : jsonResponse(_meBody, 200);
      }
      if (path.endsWith('notifications/unread-count')) {
        return jsonResponse('{"unreadCount":0}', 200);
      }
      if (path.endsWith('notifications')) {
        return jsonResponse('{"items":[],"totalCount":0}', 200);
      }
      return jsonResponse('{}', 200);
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
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: NotificationController(
        apiClient: apiClient,
        endpoint: 'student/notifications',
      ),
    );
  }

  Future<Widget> app() async {
    await session.bootstrap();
    return MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        Provider<ProfileProvider>(
          create: (_) => ProfileProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<SessionController>.value(value: session),
      ],
      child: const MaterialApp(
        home: Scaffold(body: ProfileScreen()),
      ),
    );
  }
}

Future<void> _openDeleteDialog(WidgetTester tester) async {
  await tester.scrollUntilVisible(find.text('Obriši profil'), 200);
  await tester.pumpAndSettle();
  await tester.tap(find.text('Obriši profil'));
  await tester.pumpAndSettle();
}

bool _confirmEnabled(WidgetTester tester) {
  final button = tester.widget<ElevatedButton>(
    find.widgetWithText(ElevatedButton, 'Obriši profil'),
  );
  return button.onPressed != null;
}

int _deleteRequests(ScriptedClient client) => client.requests
    .where((r) => r.method == 'DELETE' && r.url.path.endsWith('users/me'))
    .length;

void main() {
  testWidgets(
    'users/me returns 500 at boot shows AsyncStateView error with retry, retry succeeds shows content',
    (tester) async {
      final harness = _Harness(meStatus: 500);
      await tester.pumpWidget(await harness.app());
      await tester.pumpAndSettle();

      expect(find.byType(AsyncStateView), findsOneWidget);
      expect(find.text('Pokušaj ponovo'), findsOneWidget);
      expect(find.text('Student Enote'), findsNothing);

      harness.meStatus = 200;
      await tester.tap(find.text('Pokušaj ponovo'));
      await tester.pumpAndSettle();

      expect(find.byType(AsyncStateView), findsNothing);
      expect(find.text('Student Enote'), findsOneWidget);
    },
  );

  testWidgets('the delete dialog explains what is deleted and needs a '
      'password and an explicit confirm', (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(await harness.app());
    await tester.pumpAndSettle();

    await _openDeleteDialog(tester);

    expect(find.text('Brisanje profila'), findsOneWidget);
    expect(
      find.textContaining('Evidencije o uplatama, iznajmljivanjima'),
      findsOneWidget,
    );
    expect(find.text('Trenutna lozinka *'), findsOneWidget);
    expect(_confirmEnabled(tester), isFalse);

    await tester.tap(find.text('Potvrđujem trajno brisanje profila'));
    await tester.pumpAndSettle();

    expect(_confirmEnabled(tester), isTrue);
  });

  testWidgets('a wrong password renders the server message under the field '
      'and keeps the session', (tester) async {
    final harness = _Harness(
      deleteStatus: 400,
      deleteBody: {
        'status': 400,
        'code': 'error.current_password_invalid',
        'message': 'Trenutna lozinka nije ispravna.',
      },
    );
    await tester.pumpWidget(await harness.app());
    await tester.pumpAndSettle();

    await _openDeleteDialog(tester);
    await tester.enterText(find.byType(TextFormField), 'Pogresna1!');
    await tester.tap(find.text('Potvrđujem trajno brisanje profila'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Obriši profil'));
    await tester.pumpAndSettle();

    expect(find.text('Trenutna lozinka nije ispravna.'), findsOneWidget);
    expect(find.text('Brisanje profila'), findsOneWidget);
    expect(harness.authState.isAuthenticated, isTrue);
    expect(_deleteRequests(harness.client), 1);
  });

  testWidgets('a blocking reason from the server is shown in the dialog',
      (tester) async {
    final harness = _Harness(
      deleteStatus: 400,
      deleteBody: {
        'status': 400,
        'code': 'error.business',
        'message': 'Profil se ne može obrisati jer imate neizmireno dugovanje '
            'za iznajmljivanje.',
      },
    );
    await tester.pumpWidget(await harness.app());
    await tester.pumpAndSettle();

    await _openDeleteDialog(tester);
    await tester.enterText(find.byType(TextFormField), 'Password1!');
    await tester.tap(find.text('Potvrđujem trajno brisanje profila'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Obriši profil'));
    await tester.pumpAndSettle();

    expect(
      find.textContaining('Profil se ne može obrisati'),
      findsOneWidget,
    );
    expect(harness.authState.isAuthenticated, isTrue);
  });

  testWidgets('a successful deletion sends the password, ends the session and '
      'shows the snack', (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(await harness.app());
    await tester.pumpAndSettle();

    await _openDeleteDialog(tester);
    await tester.enterText(find.byType(TextFormField), 'Password1!');
    await tester.tap(find.text('Potvrđujem trajno brisanje profila'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Obriši profil'));
    await tester.pumpAndSettle();

    final delete = harness.client.requests.lastWhere(
      (r) => r.method == 'DELETE' && r.url.path.endsWith('users/me'),
    );
    expect(
      jsonDecode(delete.body),
      {'currentPassword': 'Password1!'},
    );
    expect(harness.authState.isAuthenticated, isFalse);
    expect(harness.session.profile, isNull);
    expect(find.text('Profil je obrisan.'), findsOneWidget);
    expect(find.text('Brisanje profila'), findsNothing);
  });
}
