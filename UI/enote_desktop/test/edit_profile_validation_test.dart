import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/edit_profile_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';
import 'package:enote_desktop/widgets/date_field.dart';

import 'helpers.dart';

Future<ScriptedClient> _pumpDialog(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1400, 1000);
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
}
