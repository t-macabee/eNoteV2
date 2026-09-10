import 'package:enote_core/enote_core.dart';

/// Lecture notes over `student/lectures/{lectureId}/notes` (01 §4.3).
///
/// Route-scoped: created in the `/lectures/notes` route because the endpoint
/// depends on the lecture (01 §4.2).
class LectureNoteProvider extends ReadOnlyProvider<LectureNoteDto> {
  final int lectureId;

  LectureNoteProvider({required super.apiClient, required this.lectureId})
    : super(endpoint: 'student/lectures/$lectureId/notes');

  @override
  LectureNoteDto fromJson(Map<String, dynamic> json) =>
      LectureNoteDto.fromJson(json);
}
