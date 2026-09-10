import 'package:enote_core/enote_core.dart';

/// Student lectures over `student/lectures` (01 §4.3).
class LectureProvider extends ReadOnlyProvider<LectureDto> {
  LectureProvider({required super.apiClient})
    : super(endpoint: 'student/lectures');

  @override
  LectureDto fromJson(Map<String, dynamic> json) =>
      LectureDto.fromJson(_normalize(json));

  /// The student lecture endpoints serialize `lectureStatus` (and
  /// `myAttendanceStatus`) as enum **names** (`"Scheduled"`), while the core
  /// DTO reads ints (`Observed fact` from the live API). Normalizing here —
  /// inside the Phase 7 provider — keeps the shared core model untouched.
  static Map<String, dynamic> _normalize(Map<String, dynamic> json) {
    final normalized = Map<String, dynamic>.from(json);
    normalized['lectureStatus'] = _statusValue(normalized['lectureStatus']);
    normalized['myAttendanceStatus'] = _attendanceValue(
      normalized['myAttendanceStatus'],
    );
    return normalized;
  }

  static int _statusValue(dynamic value) {
    if (value is int) return value;
    return switch (value) {
      'Scheduled' => 1,
      'Held' => 2,
      'Cancelled' => 3,
      _ => 1,
    };
  }

  static int? _attendanceValue(dynamic value) {
    if (value == null) return null;
    if (value is int) return value;
    return switch (value) {
      'Pending' => 1,
      'Present' => 2,
      'Absent' => 3,
      _ => 1,
    };
  }

  /// POST `student/lectures/{id}/rsvp` with `{confirm, note?}`.
  Future<RsvpResponse> rsvp(int id, RsvpRequest request) async {
    final response = await apiClient.post(
      '$endpoint/$id/rsvp',
      body: request.toJson(),
    );
    return RsvpResponse.fromJson(decodeOrThrow(response));
  }
}
