import 'package:enote_core/enote_core.dart';

class SubmissionProvider extends BaseProvider<AssignmentSubmissionDto> {
  SubmissionProvider({
    required super.apiClient,
    required int lectureId,
    required int assignmentId,
  }) : super(
         endpoint:
             'instructor/lectures/$lectureId/assignments/$assignmentId/submissions',
       );

  @override
  AssignmentSubmissionDto fromJson(Map<String, dynamic> json) =>
      AssignmentSubmissionDto.fromJson(json);

  Future<AssignmentSubmissionDto> grade(int submissionId, int grade) async {
    final response = await apiClient.put(
      '$endpoint/$submissionId/grade',
      body: GradeAssignmentRequest(grade: grade).toJson(),
    );

    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }
}
