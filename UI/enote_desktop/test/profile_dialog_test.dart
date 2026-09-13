import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/profile_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';

import 'helpers.dart';

class _MockProfileHttpClient extends http.BaseClient {
  final Map<String, dynamic> responseMap;
  final List<String?> putBodies = [];
  _MockProfileHttpClient(this.responseMap);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request.method == 'PUT' && request is http.Request) {
      putBodies.add(request.body);
    }
    final bytes = utf8.encode(jsonEncode(responseMap));
    return http.StreamedResponse(
      Stream.value(bytes),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('ProfileDialog renders username from UserProfileResponse',
      (tester) async {
    final client = _MockProfileHttpClient({
      'role': 'Administrator',
      'username': 'admin',
      'email': 'admin@enote.com',
      'profile': {
        r'$type': 'admin',
        'firstName': 'Ad',
        'lastName': 'Min',
      },
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
        child: const MaterialApp(
          home: Scaffold(
            body: ProfileDialog(),
          ),
        ),
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
    final client = _MockProfileHttpClient({
      'role': 'Administrator',
      'profile': {
        r'$type': 'admin',
        'firstName': null,
        'lastName': null,
      },
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
        child: const MaterialApp(
          home: Scaffold(
            body: ProfileDialog(),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Korisničko ime'), findsOneWidget);
    expect(find.text('admin'), findsOneWidget);
    expect(find.text('Uloga'), findsOneWidget);
    expect(find.text('Administrator'), findsOneWidget);
  });

  testWidgets('ProfileDialog shows initials fallback when hasPicture is false',
      (tester) async {
    final client = _MockProfileHttpClient({
      'role': 'Administrator',
      'username': 'admin',
      'email': 'admin@enote.com',
      'hasPicture': false,
      'profile': {
        r'$type': 'admin',
        'firstName': 'Ad',
        'lastName': 'Min',
      },
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
        child: const MaterialApp(
          home: Scaffold(
            body: ProfileDialog(),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('AM'), findsOneWidget);
  });

  testWidgets(
      'save the profile dialog with a blanked first name -> request body has no firstName key',
      (tester) async {
    final client = _MockProfileHttpClient({
      'role': 'Administrator',
      'username': 'admin',
      'email': 'admin@enote.com',
      'profile': {
        r'$type': 'admin',
        'firstName': 'Ad',
        'lastName': 'Min',
      },
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
        child: const MaterialApp(
          home: Scaffold(
            body: ProfileDialog(),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    await tester.tap(find.text('Uredi'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Ime'), '');
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    expect(client.putBodies, isNotEmpty);
    final putBody = jsonDecode(client.putBodies.last!) as Map<String, dynamic>;
    expect(putBody.containsKey('firstName'), isFalse);
    expect(putBody['email'], 'admin@enote.com');
  });
}
