import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/announcements/announcement_provider.dart';

import '../../helpers.dart';

const _announcementJson = {
  'id': 4,
  'courseId': 2,
  'title': 'Dodatni čas u petak',
  'content': 'Vidimo se u Sali 2.',
  'scope': 'Course',
  'courseName': 'Osnove teorije muzike',
  'publishedAt': '2026-09-12T10:00:00',
};

const _pageBody = {
  'items': [_announcementJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

AnnouncementProvider _provider(RecordingHttpClient client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return AnnouncementProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getPage issues GET student/announcements with the title query',
      () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    final page = await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'title': 'čas',
      },
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/announcements');
    expect(sent.url.queryParameters['page'], '1');
    expect(sent.url.queryParameters['pageSize'], '20');
    expect(sent.url.queryParameters['includeTotalCount'], 'true');
    expect(sent.url.queryParameters['title'], 'čas');
    expect(page.items, hasLength(1));
    expect(page.items.single.title, 'Dodatni čas u petak');
    expect(page.items.single.scope, AnnouncementScope.course);
    expect(page.items.single.courseName, 'Osnove teorije muzike');
  });
}
