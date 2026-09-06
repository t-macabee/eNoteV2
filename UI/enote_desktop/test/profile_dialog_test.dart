import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/profile_dialog.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
}) {
  final header = _base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = _base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    'exp': DateTime.now()
            .add(const Duration(days: 1))
            .millisecondsSinceEpoch ~/
        1000,
  }));
  return '$header.$payload.signature';
}

class _MockProfileHttpClient extends http.BaseClient {
  final Map<String, dynamic> responseMap;
  _MockProfileHttpClient(this.responseMap);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
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
      tokenReader: () => _fakeJwt(),
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
      tokenReader: () => _fakeJwt(username: 'admin'),
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
}
