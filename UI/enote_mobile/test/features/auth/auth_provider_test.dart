import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/auth/auth_provider.dart';

import '../../helpers.dart';

AuthState _authState(RecordingHttpClient http) => AuthState(
  baseUrl: 'http://10.0.2.2:5059/api/v1/',
  httpClient: http,
);

void main() {
  test('register posts the register body to auth/register', () async {
    final http = RecordingHttpClient(
      body: {'userId': 1, 'username': 'novi', 'roles': ['Student'], 'token': 't'},
    );
    final provider = AuthProvider(
      apiClient: ApiClient(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        authState: _authState(http),
        httpClient: http,
      ),
    );
    final result = await provider.register(
      RegisterRequest(
        username: 'novi',
        email: 'novi@enote.com',
        password: 'Test1234!',
        firstName: 'Novi',
        lastName: 'Korisnik',
      ),
    );
    expect(result.username, 'novi');
    expect(http.requests, hasLength(1));
    final request = http.requests.single;
    expect(request.method, 'POST');
    expect(request.url.path, endsWith('auth/register'));
    final body = jsonDecode(request.body) as Map<String, dynamic>;
    expect(body['username'], 'novi');
    expect(body['email'], 'novi@enote.com');
    expect(body['password'], 'Test1234!');
    expect(body['firstName'], 'Novi');
    expect(body['lastName'], 'Korisnik');
  });

  test('forgotPassword posts the email to auth/forgot-password', () async {
    final http = RecordingHttpClient(
      body: {'message': 'Poslano.'},
    );
    final provider = AuthProvider(
      apiClient: ApiClient(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        authState: _authState(http),
        httpClient: http,
      ),
    );
    await provider.forgotPassword('student@enote.com');
    expect(http.requests, hasLength(1));
    final request = http.requests.single;
    expect(request.method, 'POST');
    expect(request.url.path, endsWith('auth/forgot-password'));
    final body = jsonDecode(request.body) as Map<String, dynamic>;
    expect(body, {'email': 'student@enote.com'});
  });

  test('resetPassword posts email, token and new password', () async {
    final http = RecordingHttpClient(
      body: {'message': 'OK'},
    );
    final provider = AuthProvider(
      apiClient: ApiClient(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        authState: _authState(http),
        httpClient: http,
      ),
    );
    await provider.resetPassword(
      email: 'student@enote.com',
      token: 'abc',
      newPassword: 'Test1234!',
    );
    expect(http.requests, hasLength(1));
    final request = http.requests.single;
    expect(request.method, 'POST');
    expect(request.url.path, endsWith('auth/reset-password'));
    final body = jsonDecode(request.body) as Map<String, dynamic>;
    expect(body['email'], 'student@enote.com');
    expect(body['token'], 'abc');
    expect(body['newPassword'], 'Test1234!');
  });
}
