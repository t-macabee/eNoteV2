import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/shell/login_screen.dart';

import '../helpers.dart';

void main() {
  testWidgets('a failed login shows the banner and re-enables the form', (
    tester,
  ) async {
    final client = ScriptedClient(
      (_) => jsonResponse(const {
        'message': 'Pogrešno korisničko ime ili lozinka.',
      }, 401),
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

  testWidgets('the password visibility icon toggles the password field', (
    tester,
  ) async {
    await pumpLoginScreen(tester);

    EditableText passwordEditable() => tester.widget<EditableText>(
      find.descendant(
        of: find.byType(TextFormField).at(1),
        matching: find.byType(EditableText),
      ),
    );

    expect(passwordEditable().obscureText, isTrue);

    await tester.tap(find.byIcon(Icons.visibility_outlined));
    await tester.pump();
    expect(passwordEditable().obscureText, isFalse);

    await tester.tap(find.byIcon(Icons.visibility_off_outlined));
    await tester.pump();
    expect(passwordEditable().obscureText, isTrue);
  });

  testWidgets('the brand tagline is shown on a wide surface', (tester) async {
    await tester.binding.setSurfaceSize(const Size(1200, 800));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await pumpLoginScreen(tester);

    expect(
      find.text('Kursevi i iznajmljivanje instrumenata na jednom mjestu.'),
      findsOneWidget,
    );
  });

  testWidgets('the brand tagline is hidden on a narrow surface', (
    tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(600, 800));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await pumpLoginScreen(tester);

    expect(
      find.text('Kursevi i iznajmljivanje instrumenata na jednom mjestu.'),
      findsNothing,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('the login screen has no app bar', (tester) async {
    await pumpLoginScreen(tester);

    expect(find.byType(AppBar), findsNothing);
  });
}

Future<void> pumpLoginScreen(WidgetTester tester) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: ScriptedClient((_) => jsonResponse(const {}, 200)),
  );

  return tester.pumpWidget(
    ChangeNotifierProvider<AuthState>.value(
      value: authState,
      child: const MaterialApp(home: LoginScreen()),
    ),
  );
}
