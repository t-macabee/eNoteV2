import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

String _base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

String _fakeJwt({String subject = '1', String username = 'ana', String role = 'Instructor'}) {
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

/// Records every request sent through it and answers each one with a
/// [statusCode] and [body], so [AuthState.login] can be exercised without a
/// real backend.
class _RecordingHttpClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final int statusCode;
  final Map<String, dynamic> body;

  _RecordingHttpClient({this.statusCode = 200, this.body = const {}});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request as http.Request);
    final bytes = utf8.encode(jsonEncode(body));
    return http.StreamedResponse(
      Stream.value(bytes),
      statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  group('AuthState.login', () {
    test('a 200 with a valid JWT authenticates and populates roles', () async {
      final httpClient = _RecordingHttpClient(body: {
        'userId': 1,
        'username': 'ana',
        'roles': ['Instructor'],
        'token': _fakeJwt(role: 'Instructor'),
      });
      final authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: httpClient,
      );

      await authState.login('ana', 'password');

      expect(authState.isAuthenticated, isTrue);
      expect(authState.userId, 1);
      expect(authState.username, 'ana');
      expect(authState.roles, ['Instructor']);

      final request = httpClient.requests.single;
      expect(
        request.url.toString(),
        'http://localhost:5059/api/v1/auth/login',
      );
      expect(request.headers['Content-Type'], 'application/json');
      final sentBody = jsonDecode(request.body) as Map<String, dynamic>;
      expect(sentBody, {'username': 'ana', 'password': 'password'});
    });

    test('a 401 throws ApiException with the mapped Bosnian message', () async {
      final httpClient = _RecordingHttpClient(
        statusCode: 401,
        body: {
          'status': 401,
          'code': 'Unauthorized',
          'message': 'Pogrešno korisničko ime ili lozinka.',
        },
      );
      final authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: httpClient,
      );

      await expectLater(
        authState.login('ana', 'wrong'),
        throwsA(isA<ApiException>().having(
          (e) => e.message,
          'message',
          'Pogrešno korisničko ime ili lozinka.',
        )),
      );
      expect(authState.isAuthenticated, isFalse);
    });
  });
}
