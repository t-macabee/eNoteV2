import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/auth/auth_provider.dart';
import 'package:enote_mobile/features/auth/register_screen.dart';

import '../../helpers.dart';

void main() {
  testWidgets('the register form refuses empty names and sends no request', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(800, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final client = ScriptedClient(
      (_) => jsonResponse({
        'userId': 1,
        'username': 'novi',
        'roles': ['Student'],
        'token': fakeJwt(),
      }, 200),
    );
    final authState = AuthState(httpClient: client);
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<AuthProvider>(
            create: (_) => AuthProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: RegisterScreen()),
      ),
    );

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'novi');
    await tester.enterText(fields.at(1), 'novi@enote.com');
    await tester.enterText(fields.at(2), 'Test1234!');
    await tester.enterText(fields.at(3), 'Test1234!');
    await tester.tap(find.text('Registruj se'));
    await tester.pump();

    expect(find.text('Ime je obavezno.'), findsOneWidget);
    expect(client.requests, isEmpty);
  });
}
