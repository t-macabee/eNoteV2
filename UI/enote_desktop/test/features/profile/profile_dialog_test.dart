import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/profile_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';
import 'package:enote_desktop/theme/app_theme.dart';
import 'package:enote_desktop/widgets/dialog_shell.dart';
import 'package:enote_desktop/widgets/inline_error_text.dart';

import '../../helpers.dart';

ScriptedClient _client(Map<String, dynamic> responseMap) =>
    ScriptedClient((_) => jsonResponse(responseMap, 200));

void main() {
  profileShellTests();

  testWidgets('ProfileDialog renders username from UserProfileResponse', (
    tester,
  ) async {
    final client = _client({
      'role': 'Administrator',
      'username': 'admin',
      'email': 'admin@enote.com',
      'profile': {r'$type': 'admin', 'firstName': 'Ad', 'lastName': 'Min'},
    });

    final authState = AuthState(
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    final apiClient = ApiClient(
      baseUrl: 'http://test/api/v1/',
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
        child: const MaterialApp(home: Scaffold(body: ProfileDialog())),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Korisničko ime'), findsOneWidget);
    expect(find.text('admin'), findsOneWidget);
    expect(find.text('Uloga'), findsOneWidget);
    expect(find.text('Administrator'), findsOneWidget);
  });

  testWidgets(
    'ProfileDialog falls back to AuthState.username if response username is empty',
    (tester) async {
      final client = _client({
        'role': 'Administrator',
        'profile': {r'$type': 'admin', 'firstName': null, 'lastName': null},
      });

      final authState = AuthState(
        tokenReader: () => fakeJwt(username: 'admin'),
        httpClient: client,
      );
      final apiClient = ApiClient(
        baseUrl: 'http://test/api/v1/',
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
          child: const MaterialApp(home: Scaffold(body: ProfileDialog())),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Korisničko ime'), findsOneWidget);
      expect(find.text('admin'), findsOneWidget);
      expect(find.text('Uloga'), findsOneWidget);
      expect(find.text('Administrator'), findsOneWidget);
    },
  );

  testWidgets(
    'ProfileDialog requests the picture URL when hasPicture is false and '
    'shows initials only when the request fails',
    (tester) async {
      final client = _client({
        'role': 'Administrator',
        'username': 'admin',
        'email': 'admin@enote.com',
        'hasPicture': false,
        'profile': {r'$type': 'admin', 'firstName': 'Ad', 'lastName': 'Min'},
      });

      final authState = AuthState(
        tokenReader: () => fakeJwt(),
        httpClient: client,
      );
      final apiClient = ApiClient(
        baseUrl: 'http://test/api/v1/',
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
          child: const MaterialApp(home: Scaffold(body: ProfileDialog())),
        ),
      );

      await tester.pumpAndSettle();

      final image = tester.widget<Image>(find.byType(Image));
      expect(
        (image.image as NetworkImage).url,
        'http://test/api/v1/users/me/picture?v=1',
      );

      expect(find.text('AM'), findsOneWidget);
    },
  );

  testWidgets(
    'a blank first name shows the required message and sends no request',
    (tester) async {
      final client = _client({
        'role': 'Administrator',
        'username': 'admin',
        'email': 'admin@enote.com',
        'profile': {r'$type': 'admin', 'firstName': 'Ad', 'lastName': 'Min'},
      });

      final authState = AuthState(
        tokenReader: () => fakeJwt(),
        httpClient: client,
      );
      final apiClient = ApiClient(
        baseUrl: 'http://test/api/v1/',
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
          child: const MaterialApp(home: Scaffold(body: ProfileDialog())),
        ),
      );

      await tester.pumpAndSettle();

      await tester.tap(find.text('Uredi'));
      await tester.pumpAndSettle();

      await tester.enterText(find.widgetWithText(TextFormField, 'Ime *'), '');
      await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
      await tester.pumpAndSettle();

      expect(find.text('Ime je obavezno.'), findsOneWidget);
      expect(client.putBodies, isEmpty);
      expect(find.text('Uredi profil'), findsOneWidget);
    },
  );
}

Future<void> _pumpProfile(WidgetTester tester, ScriptedClient client) async {
  final authState = AuthState(tokenReader: () => fakeJwt(), httpClient: client);
  final apiClient = ApiClient(
    baseUrl: 'http://test/api/v1/',
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
        theme: desktopTheme,
        home: const Scaffold(body: ProfileDialog()),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void profileShellTests() {
  testWidgets(
    'ProfileDialog uses the shared dialog shell with a sentence-case title and a dash for missing values',
    (tester) async {
      await _pumpProfile(
        tester,
        _client({
          'role': 'Instructor',
          'username': 'instructor',
          'email': 'instructor@enote.com',
          'profile': {
            r'$type': 'instructor',
            'firstName': 'Selma',
            'lastName': 'Begić',
          },
        }),
      );

      expect(find.byType(DialogShell), findsOneWidget);
      expect(find.text('Moj profil'), findsOneWidget);
      expect(find.text('Instruktor'), findsOneWidget);
      expect(
        find.descendant(
          of: find.widgetWithText(InfoRow, 'Datum rođenja'),
          matching: find.text('—'),
        ),
        findsOneWidget,
      );
      expect(find.byType(InfoRow), findsNWidgets(6));
      for (final row in tester.widgetList<InfoRow>(find.byType(InfoRow))) {
        expect(row.icon, isNotNull);
      }
    },
  );

  testWidgets(
    'ProfileDialog shows the shared inline error when the fetch fails',
    (tester) async {
      await _pumpProfile(
        tester,
        ScriptedClient((_) => jsonResponse({'message': 'boom'}, 500)),
      );

      expect(find.byType(DialogShell), findsOneWidget);
      expect(
        find.widgetWithText(InlineErrorText, 'Greška pri učitavanju profila.'),
        findsOneWidget,
      );
    },
  );
}
