import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/lecture_notes/lecture_note_provider.dart';

import '../../helpers.dart';

const _noteJson = {
  'id': 4,
  'lectureId': 11,
  'title': 'Skale',
  'content': 'C-dur skala kroz dvije oktave.',
};

const _pageBody = {
  'items': [_noteJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

LectureNoteProvider _provider(RecordingHttpClient client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return LectureNoteProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
    lectureId: 11,
  );
}

void main() {
  test('getPage issues GET student/lectures/{id}/notes with title query',
      () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    final page = await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'title': 'skale',
      },
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/lectures/11/notes');
    expect(sent.url.queryParameters['title'], 'skale');
    expect(page.items, hasLength(1));
    expect(page.items.single.title, 'Skale');
  });

  test('getById issues GET student/lectures/{id}/notes/{noteId}', () async {
    final client = RecordingHttpClient(body: _noteJson);
    final provider = _provider(client);

    final note = await provider.getById(4);

    expect(
      client.requests.single.url.path,
      '/api/v1/student/lectures/11/notes/4',
    );
    expect(note.content, 'C-dur skala kroz dvije oktave.');
  });
}
