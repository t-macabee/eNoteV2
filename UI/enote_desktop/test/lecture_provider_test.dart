import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';

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
}
