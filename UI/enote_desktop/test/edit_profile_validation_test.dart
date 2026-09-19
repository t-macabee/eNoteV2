import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/edit_profile_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';
import 'package:enote_desktop/widgets/date_field.dart';

import 'helpers.dart';

Future<ScriptedClient> _pumpDialog(
  WidgetTester tester, {
  int status = 200,
  Map<String, dynamic> body = const {'message': 'OK'},
}) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = ScriptedClient((_) => jsonResponse(body, status));
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        Provider<ProfileProvider>(
          create: (_) => ProfileProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(
        home: Scaffold(body: EditProfileDialog(initialEmail: 'a@b.com')),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

Future<void> _save(WidgetTester tester) async {
  await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('F4-10: malformed email shows the format validator text',
      (tester) async {
    final client = await _pumpDialog(tester);

    await tester.enterText(
      find.widgetWithText(TextFormField, 'Email'),
      'not-an-email',
    );
    await tester.pump();
    await _save(tester);

    expect(find.text('Unesite važeću email adresu.'), findsOneWidget);
    expect(client.requests.where((r) => r.method == 'PUT'), isEmpty);
  });

  testWidgets('F4-11: future date of birth shows the validator text',
      (tester) async {
    final client = await _pumpDialog(tester);

    final state = tester.state<FormFieldState<DateTime?>>(
      find.byType(DateField),
    );
    state.didChange(DateTime.now().add(const Duration(days: 1)));
    await tester.pump();
    await _save(tester);

    expect(
      find.text('Datum rođenja ne može biti u budućnosti.'),
      findsOneWidget,
    );
    expect(client.requests.where((r) => r.method == 'PUT'), isEmpty);
  });

  testWidgets('a failed save shows the banner and keeps the dialog open',
      (tester) async {
    await _pumpDialog(
      tester,
      status: 500,
      body: const {'message': 'Greška na serveru.'},
    );

    await _save(tester);

    expect(find.text('Greška na serveru.'), findsOneWidget);
    expect(find.text('Uredi profil'), findsOneWidget);
  });
}
