import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/change_password_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';

import 'helpers.dart';

Future<ScriptedClient> _pumpDialog(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1400, 900);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = ScriptedClient((_) => jsonResponse({'message': 'OK'}, 200));
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
      child: MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => FilledButton(
              onPressed: () => showDialog<void>(
                context: context,
                builder: (_) => const ChangePasswordDialog(),
              ),
              child: const Text('otvori'),
            ),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('otvori'));
  await tester.pumpAndSettle();
  return client;
}

Future<void> _save(WidgetTester tester) async {
  await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('F4-08: weak new password shows the strength validator text',
      (tester) async {
    final client = await _pumpDialog(tester);

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Stara1!');
    await tester.enterText(fields.at(1), 'abc');
    await tester.enterText(fields.at(2), 'abc');
    await tester.pump();
    await _save(tester);

    expect(
      find.text('Lozinka mora imati najmanje 8 znakova.'),
      findsOneWidget,
    );
    expect(client.requests.where((r) => r.method == 'PUT'), isEmpty);
  });

  testWidgets(
      'F4-09: changing the new password after confirming shows a mismatch '
      'without any rebuild hack', (tester) async {
    final client = await _pumpDialog(tester);

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Stara11!');
    await tester.enterText(fields.at(1), 'Test1234!');
    await tester.pump();
    await tester.enterText(fields.at(2), 'Test1234!');
    await tester.pump();
    // Edit the new password after the confirmation was entered. The live
    // confirm validator must notice without a forced rebuild.
    await tester.enterText(fields.at(1), 'Drugacij1!');
    await tester.pump();
    await _save(tester);

    expect(
      find.text('Lozinke se ne podudaraju.'),
      findsOneWidget,
    );
    expect(client.requests.where((r) => r.method == 'PUT'), isEmpty);
  });
}
