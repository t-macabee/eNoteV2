import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/profile/change_password_screen.dart';
import 'package:enote_mobile/features/profile/profile_provider.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

class _PasswordStubClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final bool failWithWrongPassword;

  _PasswordStubClient({this.failWithWrongPassword = false});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request is http.Request) {
      requests.add(request);
    }
    final isPasswordPut = request.method == 'PUT' &&
        request.url.path == '/api/v1/users/me/password';
    final body = isPasswordPut && failWithWrongPassword
        ? {'message': 'Pogrešna trenutna lozinka.'}
        : <String, dynamic>{'message': 'OK'};
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      isPasswordPut && failWithWrongPassword ? 400 : 200,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _Harness {
  final _PasswordStubClient client;
  late final ApiClient apiClient;

  _Harness({bool failWithWrongPassword = false})
    : client = _PasswordStubClient(
        failWithWrongPassword: failWithWrongPassword,
      ) {
    final authState = AuthState(
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
  }

  Widget app(Widget home) {
    return MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        Provider<ProfileProvider>(
          create: (_) => ProfileProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(theme: AppTheme.dark, home: home),
    );
  }
}

Widget _host() {
  return Scaffold(
    body: Builder(
      builder: (context) => Center(
        child: FilledButton(
          onPressed: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const ChangePasswordScreen()),
          ),
          child: const Text('otvori'),
        ),
      ),
    ),
  );
}

Future<void> _openScreen(WidgetTester tester) async {
  await tester.tap(find.text('otvori'));
  await tester.pumpAndSettle();
}

Future<void> _fill(
  WidgetTester tester, {
  required String current,
  required String next,
  required String confirm,
}) async {
  final fields = find.byType(TextFormField);
  await tester.enterText(fields.at(0), current);
  await tester.enterText(fields.at(1), next);
  await tester.enterText(fields.at(2), confirm);
  await tester.pump();
}

void main() {
  testWidgets('an empty current password shows its own message only',
      (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.app(_host()));
    await _openScreen(tester);

    await _fill(
      tester,
      current: '',
      next: 'Test1234!',
      confirm: 'Test1234!',
    );

    expect(find.text('Trenutna lozinka je obavezan.'), findsOneWidget);
    expect(find.text('Lozinka i potvrda se ne poklapaju.'), findsNothing);
  });

  testWidgets('a weak new password shows its own message only', (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.app(_host()));
    await _openScreen(tester);

    await _fill(tester, current: 'Stara1!', next: 'abc', confirm: 'abc');

    expect(
      find.text('Lozinka mora imati najmanje 8 karaktera.'),
      findsOneWidget,
    );
    expect(find.text('Trenutna lozinka je obavezan.'), findsNothing);
    expect(find.text('Lozinka i potvrda se ne poklapaju.'), findsNothing);
  });

  testWidgets('a mismatched confirmation shows its own message only',
      (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.app(_host()));
    await _openScreen(tester);

    await _fill(
      tester,
      current: 'Stara1!',
      next: 'Test1234!',
      confirm: 'Nesto9999',
    );

    expect(
      find.text('Lozinka i potvrda se ne poklapaju.'),
      findsOneWidget,
    );
    expect(find.text('Trenutna lozinka je obavezna.'), findsNothing);
  });

  testWidgets('submit stays disabled until all fields are valid and then '
      'issues the PUT on success', (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.app(_host()));
    await _openScreen(tester);

    FilledButton submitButton() => tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Promijeni lozinku'),
    );

    expect(submitButton().onPressed, isNull);

    await _fill(
      tester,
      current: 'Stara1!',
      next: 'Test1234!',
      confirm: 'Test1234!',
    );
    expect(submitButton().onPressed, isNotNull);

    await tester.tap(find.text('Promijeni lozinku'));
    await tester.pumpAndSettle();

    expect(harness.client.requests, hasLength(1));
    final sent = harness.client.requests.single;
    expect(sent.method, 'PUT');
    expect(sent.url.path, '/api/v1/users/me/password');
    final body = jsonDecode(sent.body) as Map<String, dynamic>;
    expect(body, {
      'currentPassword': 'Stara1!',
      'newPassword': 'Test1234!',
      'confirmNewPassword': 'Test1234!',
    });
    expect(find.text('Lozinka je uspješno promijenjena.'), findsOneWidget);

    await tester.pump(const Duration(seconds: 5));
    await tester.pumpAndSettle();
  });

  testWidgets('a 400 from the server renders verbatim under the button',
      (tester) async {
    final harness = _Harness(failWithWrongPassword: true);
    await tester.pumpWidget(harness.app(_host()));
    await _openScreen(tester);

    await _fill(
      tester,
      current: 'Krivo1!',
      next: 'Test1234!',
      confirm: 'Test1234!',
    );
    await tester.tap(find.text('Promijeni lozinku'));
    await tester.pumpAndSettle();

    expect(find.text('Pogrešna trenutna lozinka.'), findsOneWidget);
    expect(find.text('Promjena lozinke'), findsOneWidget);
  });
}
