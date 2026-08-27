import 'package:enote_core/enote_core.dart';

class LectureNoteProvider extends BaseProvider<LectureNoteDto> {
  LectureNoteProvider({required super.apiClient, required int lectureId})
      : super(endpoint: 'instructor/lectures/$lectureId/notes');

  @override
  LectureNoteDto fromJson(Map<String, dynamic> json) =>
      LectureNoteDto.fromJson(json);
}
