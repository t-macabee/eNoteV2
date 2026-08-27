import 'package:enote_core/enote_core.dart';

class AssignmentProvider extends BaseProvider<AssignmentDto> {
  AssignmentProvider({required super.apiClient, required int lectureId})
      : super(endpoint: 'instructor/lectures/$lectureId/assignments');

  @override
  AssignmentDto fromJson(Map<String, dynamic> json) =>
      AssignmentDto.fromJson(json);
}
