import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_mobile/session/session_http_client.dart';

class _StatusClient extends http.BaseClient {
  final int status;

  _StatusClient(this.status);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(const Stream<List<int>>.empty(), status);
  }
}

http.Request _request(String path, {String? token}) {
  final request = http.Request(
    'GET',
    Uri.parse('http://10.0.2.2:5059/api/v1/$path'),
  );
  if (token != null) {
    request.headers['Authorization'] = 'Bearer $token';
  }
  return request;
}

void main() {
  test(
    '401 on a student path calls onUnauthorized once and returns the response',
    () async {
      var calls = 0;
      final client = SessionHttpClient(
        inner: _StatusClient(401),
        onUnauthorized: () => calls++,
      );
      final response = await client.send(
        _request('student/courses', token: 't1'),
      );
      expect(response.statusCode, 401);
      expect(calls, 1);
    },
  );

  test('401 on an auth path does not call onUnauthorized', () async {
    var calls = 0;
    final client = SessionHttpClient(
      inner: _StatusClient(401),
      onUnauthorized: () => calls++,
    );
    final response = await client.send(_request('auth/login', token: 't1'));
    expect(response.statusCode, 401);
    expect(calls, 0);
  });

  test(
    'a second 401 with the same token is suppressed, a new token calls again',
    () async {
      var calls = 0;
      final client = SessionHttpClient(
        inner: _StatusClient(401),
        onUnauthorized: () => calls++,
      );
      await client.send(_request('student/courses', token: 't1'));
      await client.send(_request('student/lectures', token: 't1'));
      expect(calls, 1);
      await client.send(_request('student/courses', token: 't2'));
      expect(calls, 2);
    },
  );

  test('a non-401 response never calls onUnauthorized', () async {
    var calls = 0;
    final client = SessionHttpClient(
      inner: _StatusClient(200),
      onUnauthorized: () => calls++,
    );
    await client.send(_request('student/courses', token: 't1'));
    expect(calls, 0);
  });

  test('the default timeout is 20 seconds', () {
    expect(
      SessionHttpClient(inner: _StatusClient(200)).timeout,
      const Duration(seconds: 20),
    );
  });

  test('a hung request raises TimeoutException', () async {
    final never = Completer<http.StreamedResponse>();
    final client = SessionHttpClient(
      inner: _NeverClient(never.future),
      timeout: const Duration(milliseconds: 50),
    );
    await expectLater(
      client.send(_request('student/courses')),
      throwsA(isA<TimeoutException>()),
    );
  });
}

class _NeverClient extends http.BaseClient {
  final Future<http.StreamedResponse> future;

  _NeverClient(this.future);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) => future;
}
