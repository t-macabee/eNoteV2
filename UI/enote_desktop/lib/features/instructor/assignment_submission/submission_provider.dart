import 'dart:convert';

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

    if (response.statusCode >= 400) {
      throw ApiException(
        ApiErrorMapper.mapError(response.statusCode, response.body),
      );
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    notifyListeners();
    return fromJson(data);
  }
}
