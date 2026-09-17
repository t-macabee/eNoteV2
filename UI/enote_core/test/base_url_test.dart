import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'helpers.dart';

void main() {
  group('ApiClient base url', () {
    test('slash-less base still builds the full path', () async {
      final auth = AuthState();
      final client = RecordingHttpClient(body: {'items': []});
      final api = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1',
        authState: auth,
        httpClient: client,
      );

      await api.get('courses');

      expect(client.requests.single.url.path, '/api/v1/courses');
    });
  });

  group('AuthState base url', () {
    test('slash-less base still posts to auth/login', () async {
      final client = RecordingHttpClient(body: {
        'userId': 1,
        'username': 'ana',
        'roles': ['Instructor'],
        'token': fakeJwt(role: 'Instructor'),
      });
      final auth = AuthState(
        baseUrl: 'http://localhost:5059/api/v1',
        httpClient: client,
      );

      await auth.login('ana', 'password');

      expect(client.requests.single.url.toString(),
          'http://localhost:5059/api/v1/auth/login');
    });
  });
}
