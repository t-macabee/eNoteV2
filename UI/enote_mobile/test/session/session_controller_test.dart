import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/session/session_controller.dart';

import '../helpers.dart';

class _StubClient extends http.BaseClient {
  final Map<String, http.Response> responses;
  final List<http.BaseRequest> sent = [];
  final bool throwOnPost;

  _StubClient({required this.responses, this.throwOnPost = false});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    sent.add(request);
    if (throwOnPost && request.method == 'POST') {
      throw http.ClientException('offline');
    }
    final key = '${request.method} ${request.url.path}';
    final response =
        responses[key] ?? http.Response('{"message":"Nepoznato."}', 404);
    return http.StreamedResponse(
      Stream.value(utf8.encode(response.body)),
      response.statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

http.Response _json(Object body) =>
    http.Response(jsonEncode(body), 200, headers: {'content-type': 'application/json'});

Map<String, http.Response> _bootstrapResponses() => {
  'GET /api/v1/users/me': _json({
    'role': 'Student',
    'username': 'student',
    'email': 'student@enote.com',
    'profile': {'id': 7},
    'hasPicture': false,
  }),
  'GET /api/v1/student/notifications/unread-count': _json({'unreadCount': 2}),
  'GET /api/v1/student/notifications': _json({
    'items': [],
    'totalCount': 0,
  }),
  'POST /api/v1/auth/logout': _json({'message': 'OK'}),
};

Map<String, http.Response> _profileResponses(String? paidUntilIso) => {
  'GET /api/v1/users/me': _json({
    'role': 'Student',
    'username': 'student',
    'email': 'student@enote.com',
    'profile': {
      'id': 7,
      'membershipPaidUntil': ?paidUntilIso,
    },
    'hasPicture': false,
  }),
  'GET /api/v1/student/notifications/unread-count': _json({'unreadCount': 0}),
  'GET /api/v1/student/notifications': _json({
    'items': [],
    'totalCount': 0,
  }),
  'POST /api/v1/auth/logout': _json({'message': 'OK'}),
};

String _iso(DateTime value) => value.toIso8601String();

SessionController _controller(_StubClient http) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: http,
  );
  final apiClient = ApiClient(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    authState: authState,
    httpClient: http,
  );
  return SessionController(
    apiClient: apiClient,
    authState: authState,
    notifications: NotificationController(
      apiClient: apiClient,
      endpoint: 'student/notifications',
    ),
  );
}

void main() {
  test('bootstrap issues the three requests and caches the profile', () async {
    final http = _StubClient(responses: _bootstrapResponses());
    final controller = _controller(http);
    await controller.bootstrap();
    final paths = http.sent
        .map((r) => '${r.method} ${r.url.path}')
        .toList();
    expect(paths, contains('GET /api/v1/users/me'));
    expect(paths, contains('GET /api/v1/student/notifications/unread-count'));
    expect(paths, contains('GET /api/v1/student/notifications'));
    expect(controller.profile?.username, 'student');
    expect(controller.profile?.profile.id, 7);
  });

  test('logoutAndRevoke clears local state when auth/logout throws', () async {
    final http = _StubClient(
      responses: _bootstrapResponses(),
      throwOnPost: true,
    );
    final controller = _controller(http);
    await controller.bootstrap();
    expect(controller.authState.isAuthenticated, isTrue);
    await controller.logoutAndRevoke();
    expect(controller.authState.isAuthenticated, isFalse);
  });

  test('logoutAndRevoke posts to auth/logout on the happy path', () async {
    final http = _StubClient(responses: _bootstrapResponses());
    final controller = _controller(http);
    await controller.bootstrap();
    await controller.logoutAndRevoke();
    expect(
      http.sent.map((r) => '${r.method} ${r.url.path}'),
      contains('POST /api/v1/auth/logout'),
    );
    expect(controller.authState.isAuthenticated, isFalse);
  });

  test('isMembershipActive is false when no paid-until date is set',
      () async {
    final http = _StubClient(responses: _profileResponses(null));
    final controller = _controller(http);
    await controller.bootstrap();

    expect(controller.membershipPaidUntil, isNull);
    expect(controller.isMembershipActive, isFalse);
  });

  test('isMembershipActive is false for yesterday 23:59', () async {
    final now = DateTime.now();
    final yesterdayLate = DateTime(
      now.year,
      now.month,
      now.day - 1,
      23,
      59,
      59,
    );
    final http = _StubClient(responses: _profileResponses(_iso(yesterdayLate)));
    final controller = _controller(http);
    await controller.bootstrap();

    expect(controller.isMembershipActive, isFalse);
  });

  test('isMembershipActive is true for today 00:00 (inclusive day)', () async {
    final now = DateTime.now();
    final todayStart = DateTime(now.year, now.month, now.day);
    final http = _StubClient(responses: _profileResponses(_iso(todayStart)));
    final controller = _controller(http);
    await controller.bootstrap();

    expect(controller.membershipPaidUntil, todayStart);
    expect(controller.isMembershipActive, isTrue);
  });

  test('isMembershipActive is true for today 23:59:59 local', () async {
    final now = DateTime.now();
    final todayLate = DateTime(now.year, now.month, now.day, 23, 59, 59);
    final http = _StubClient(responses: _profileResponses(_iso(todayLate)));
    final controller = _controller(http);
    await controller.bootstrap();

    expect(controller.isMembershipActive, isTrue);
  });

  test('pictureVersion bumps on every successful reloadProfile', () async {
    final http = _StubClient(responses: _profileResponses(null));
    final controller = _controller(http);
    await controller.bootstrap();
    expect(controller.pictureVersion, 0);

    await controller.reloadProfile();
    expect(controller.pictureVersion, 1);
    await controller.reloadProfile();
    expect(controller.pictureVersion, 2);
  });
}
