import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/change_password_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';

import 'helpers.dart';

class _PasswordStubClient extends http.BaseClient {
  final List<http.Request> requests = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request is http.Request) {
      requests.add(request);
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode({'message': 'OK'}))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('a successful change logs out with the re-login snack',
      (tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = _PasswordStubClient();
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
    expect(find.text('Promijeni lozinku'), findsWidgets);

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Stara1!');
    await tester.pump();
    await tester.enterText(fields.at(1), 'Test1234!');
    await tester.pump();
    await tester.enterText(fields.at(2), 'Test1234!');
    await tester.pump();

    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(authState.isAuthenticated, isFalse);
    expect(
      find.text('Lozinka je promijenjena. Prijavite se ponovo.'),
      findsOneWidget,
    );
    expect(find.text('Promijeni lozinku'), findsNothing);
  });
}
