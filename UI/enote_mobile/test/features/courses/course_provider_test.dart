import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';

import '../../helpers.dart';

const _courseJson = {
  'id': 1,
  'instructorId': 2,
  'name': 'Osnove teorije muzike',
  'isPublished': true,
  'price': 800.0,
  'enrolledCount': 14,
  'instructorName': 'Amir Hadzic',
  'isEnrolled': true,
};

const _pageBody = {
  'items': [_courseJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

CourseProvider _provider(RecordingHttpClient client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return CourseProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getPage issues GET student/courses with paging + name query',
      () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    final page = await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'name': 'teorije',
      },
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/courses');
    expect(sent.url.queryParameters['page'], '1');
    expect(sent.url.queryParameters['pageSize'], '20');
    expect(sent.url.queryParameters['includeTotalCount'], 'true');
    expect(sent.url.queryParameters['name'], 'teorije');
    expect(page.items, hasLength(1));
    expect(page.items.single.name, 'Osnove teorije muzike');
    expect(page.items.single.isEnrolled, isTrue);
  });

  test('getPage forwards the enrolledOnly filter', () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'enrolledOnly': true,
      },
    );

    expect(client.requests, hasLength(1));
    expect(
      client.requests.single.url.queryParameters['enrolledOnly'],
      'true',
    );
  });

  test('getById issues GET student/courses/{id}', () async {
    final client = RecordingHttpClient(body: _courseJson);
    final provider = _provider(client);

    final course = await provider.getById(1);

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'GET');
    expect(client.requests.single.url.path, '/api/v1/student/courses/1');
    expect(course.name, 'Osnove teorije muzike');
  });

  test('enroll issues POST student/courses/{id}/enroll', () async {
    final client = RecordingHttpClient(body: const {});
    final provider = _provider(client);

    await provider.enroll(1);

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'POST');
    expect(
      client.requests.single.url.path,
      '/api/v1/student/courses/1/enroll',
    );
  });

  test('unenroll issues POST student/courses/{id}/unenroll', () async {
    final client = RecordingHttpClient(body: const {});
    final provider = _provider(client);

    await provider.unenroll(1);

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'POST');
    expect(
      client.requests.single.url.path,
      '/api/v1/student/courses/1/unenroll',
    );
  });
}
