import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';

import '../../helpers.dart';

const _lectureJson = {
  'id': 11,
  'name': 'Akordi I',
  'location': 'Sala 2',
  'lectureType': 'Theoretical',
  'lectureStatus': 1,
  'isCancelled': false,
  'lectureTime': '2026-09-14T18:00:00',
  'duration': 60,
  'capacity': 20,
  'attendeeCount': 5,
  'myAttendanceStatus': 1,
};

const _pageBody = {
  'items': [_lectureJson],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

LectureProvider _provider(RecordingHttpClient client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return LectureProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getPage issues GET student/lectures with the filter query', () async {
    final client = RecordingHttpClient(body: _pageBody);
    final provider = _provider(client);

    final page = await provider.getPage(
      params: {
        'page': 1,
        'pageSize': 20,
        'includeTotalCount': true,
        'courseId': 2,
        'name': 'akordi',
        'lectureType': 'Theoretical',
        'from': '2026-09-09T00:00:00.000',
        'to': '2026-09-30T00:00:00.000',
      },
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'GET');
    expect(sent.url.path, '/api/v1/student/lectures');
    expect(sent.url.queryParameters['courseId'], '2');
    expect(sent.url.queryParameters['name'], 'akordi');
    expect(sent.url.queryParameters['lectureType'], 'Theoretical');
    expect(sent.url.queryParameters['from'], '2026-09-09T00:00:00.000');
    expect(sent.url.queryParameters['to'], '2026-09-30T00:00:00.000');
    expect(page.items, hasLength(1));
    expect(page.items.single.name, 'Akordi I');
  });

  test('getById issues GET student/lectures/{id}', () async {
    final client = RecordingHttpClient(body: _lectureJson);
    final provider = _provider(client);

    final lecture = await provider.getById(11);

    expect(client.requests.single.url.path, '/api/v1/student/lectures/11');
    expect(lecture.location, 'Sala 2');
  });

  test('fromJson maps string statuses from the live API', () async {
    final client = RecordingHttpClient(
      body: {
        ..._lectureJson,
        'lectureStatus': 'Cancelled',
        'isCancelled': true,
        'myAttendanceStatus': 'Present',
      },
    );
    final provider = _provider(client);

    final lecture = await provider.getById(11);

    expect(lecture.lectureStatus, LectureStatus.cancelled);
    expect(lecture.myAttendanceStatus, AttendanceStatus.present);
  });

  test('fromJson keeps a null attendance as unanswered', () async {
    final client = RecordingHttpClient(
      body: {..._lectureJson, 'myAttendanceStatus': null},
    );
    final provider = _provider(client);

    final lecture = await provider.getById(11);

    expect(lecture.myAttendanceStatus, isNull);
  });

  test('rsvp POSTs {confirm, note} and decodes the response', () async {
    final client = RecordingHttpClient(
      body: {'lectureId': 11, 'studentId': 7, 'confirmed': true},
    );
    final provider = _provider(client);

    final result = await provider.rsvp(
      11,
      RsvpRequest(confirm: true, note: 'Dolazim'),
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/v1/student/lectures/11/rsvp');
    final body = jsonDecode(sent.body) as Map<String, dynamic>;
    expect(body, {'confirm': true, 'note': 'Dolazim'});
    expect(result.confirmed, isTrue);
    expect(result.lectureId, 11);
  });

  test('rsvp omits a null note from the body', () async {
    final client = RecordingHttpClient(
      body: {'lectureId': 11, 'studentId': 7, 'confirmed': false},
    );
    final provider = _provider(client);

    await provider.rsvp(11, RsvpRequest(confirm: false));

    final body =
        jsonDecode(client.requests.single.body) as Map<String, dynamic>;
    expect(body, {'confirm': false});
  });
}
