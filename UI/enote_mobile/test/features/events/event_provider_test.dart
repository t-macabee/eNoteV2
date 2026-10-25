import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/events/event_provider.dart';

import '../../helpers.dart';

const _eventJson = {
  'id': 6,
  'title': 'Koncert škole',
  'description': 'Godišnji koncert.',
  'startsAt': '2026-10-15T19:00:00',
  'endsAt': '2026-10-15T21:00:00',
  'addressStreet': 'Zmaja od Bosne 8',
  'addressCity': 'Mostar',
};

const _pageBody = {
  'items': [_eventJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

EventProvider _provider(RecordingHttpClient client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return EventProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getPage issues GET student/events with the from/to query', () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    final page = await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'from': '2026-10-01T00:00:00.000',
        'to': '2026-10-31T00:00:00.000',
      },
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/events');
    expect(sent.url.queryParameters['page'], '1');
    expect(sent.url.queryParameters['pageSize'], '20');
    expect(sent.url.queryParameters['includeTotalCount'], 'true');
    expect(sent.url.queryParameters['from'], '2026-10-01T00:00:00.000');
    expect(sent.url.queryParameters['to'], '2026-10-31T00:00:00.000');
    expect(page.items, hasLength(1));
    expect(page.items.single.title, 'Koncert škole');
  });

  test('an event parses without a course scope', () {
    final event = EventDto.fromJson(_eventJson);

    expect(event.id, 6);
    expect(event.addressCity, 'Mostar');
    expect(event.toJson().containsKey('courseId'), isFalse);
  });
}
