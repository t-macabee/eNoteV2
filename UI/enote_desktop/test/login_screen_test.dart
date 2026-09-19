import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/shell/login_screen.dart';

import 'helpers.dart';

void main() {
  testWidgets('a failed login shows the banner and re-enables the form',
      (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        const {'message': 'Pogrešno korisničko ime ili lozinka.'},
        401,
      ),
    );
    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthState>.value(
        value: authState,
        child: const MaterialApp(home: LoginScreen()),
      ),
    );

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'admin');
    await tester.enterText(fields.at(1), 'pogresna');
    await tester.tap(find.text('Prijavi se'));
    await tester.pumpAndSettle();

    expect(find.text('Pogrešno korisničko ime ili lozinka.'), findsOneWidget);
    final button = tester.widget<ElevatedButton>(
      find.widgetWithText(ElevatedButton, 'Prijavi se'),
    );
    expect(button.onPressed, isNotNull);
  });
}
