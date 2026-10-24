import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';

import 'helpers.dart';

Map<String, dynamic> _cancelledLectureJson() => {
  'id': 9,
  'name': 'Harmonija',
  'location': 'Sala 1',
  'lectureType': 'Theoretical',
  'lectureStatus': 'Cancelled',
  'isCancelled': true,
  'cancellationReason': 'Instrument je pokvaren',
  'cancelledAt': '2026-09-14T18:00:00',
  'lectureTime': '2026-09-14T18:00:00',
  'duration': 60,
  'attendeeCount': 0,
};

void main() {
  test('F3-09: LectureProvider exposes the attendance report route', () {
    final provider = LectureProvider(
      apiClient: ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
      ),
    );

    expect(
      provider.attendanceReportEndpoint(9),
      'instructor/lectures/9/attendance/report',
    );
  });

  test('cancel posts the reason to the cancel endpoint', () async {
    final client = RecordingHttpClient(body: _cancelledLectureJson());
    final provider = LectureProvider(
      apiClient: ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
        httpClient: client,
      ),
    );

    final lecture = await provider.cancel(9, 'Instrument je pokvaren');

    final sent = client.requests.single;
    expect(sent.method, 'POST');
    expect(sent.url.path, '/api/v1/instructor/lectures/9/cancel');
    expect(jsonDecode(sent.body), {'reason': 'Instrument je pokvaren'});
    expect(lecture.isCancelled, isTrue);
    expect(lecture.cancellationReason, 'Instrument je pokvaren');
  });
}
