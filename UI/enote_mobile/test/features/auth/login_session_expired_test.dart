import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/auth/login_screen.dart';
import 'package:enote_mobile/main.dart';

import '../../helpers.dart';

const _sessionText = 'Vaša sesija je istekla. Prijavite se ponovo.';

AuthState _signedIn() => AuthState(
  tokenReader: () => fakeJwt(),
  httpClient: ScriptedClient((_) => jsonResponse(const {}, 200)),
);

Widget _login(AuthState authState) => ChangeNotifierProvider<AuthState>.value(
  value: authState,
  child: const MaterialApp(home: LoginScreen()),
);

void main() {
  testWidgets('the login shows the session text after handleSessionExpired', (
    tester,
  ) async {
    final authState = _signedIn();

    handleSessionExpired(authState);
    await tester.pumpWidget(_login(authState));

    expect(authState.isAuthenticated, isFalse);
    expect(find.text(_sessionText), findsOneWidget);
  });

  testWidgets('the login shows no session text after a plain logout', (
    tester,
  ) async {
    final authState = _signedIn();

    await authState.logout();
    await tester.pumpWidget(_login(authState));

    expect(find.text(_sessionText), findsNothing);
  });
}
