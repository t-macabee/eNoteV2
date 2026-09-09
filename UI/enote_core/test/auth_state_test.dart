import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'helpers.dart' as helpers;

void main() {
  group('AuthState.login', () {
    test('a 200 with a valid JWT authenticates and populates roles', () async {
      final httpClient = helpers.RecordingHttpClient(body: {
        'userId': 1,
        'username': 'ana',
        'roles': ['Instructor'],
        'token': helpers.fakeJwt(role: 'Instructor'),
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
      final httpClient = helpers.RecordingHttpClient(
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

    test('is_manager claim is correctly decoded into authState.isManager', () async {
      final httpClient = helpers.RecordingHttpClient(body: {
        'userId': 2,
        'username': 'manager_bob',
        'roles': ['StoreEmployee'],
        'token': helpers.fakeJwt(
          subject: '2',
          username: 'manager_bob',
          role: 'StoreEmployee',
          isManager: true,
        ),
      });
      final authState = AuthState(
        baseUrl: 'http://localhost:5059/api/v1/',
        httpClient: httpClient,
      );

      await authState.login('manager_bob', 'password');

      expect(authState.isAuthenticated, isTrue);
      expect(authState.isManager, isTrue);

      await authState.logout();
      expect(authState.isManager, isFalse);
    });
  });
}
