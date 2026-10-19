import 'package:enote_core/enote_core.dart';

class LectureNoteProvider extends ReadOnlyProvider<LectureNoteDto> {
  final int lectureId;

  LectureNoteProvider({required super.apiClient, required this.lectureId})
    : super(endpoint: 'student/lectures/$lectureId/notes');

  @override
  LectureNoteDto fromJson(Map<String, dynamic> json) =>
      LectureNoteDto.fromJson(json);
}
