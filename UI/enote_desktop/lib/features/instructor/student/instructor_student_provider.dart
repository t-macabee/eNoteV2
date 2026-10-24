import 'package:enote_core/enote_core.dart';

class InstructorStudentProvider extends BaseProvider<StudentDto>
    with MembershipPaymentsApi<StudentDto> {
  InstructorStudentProvider({required super.apiClient})
      : super(endpoint: 'instructor/students');

  @override
  StudentDto fromJson(Map<String, dynamic> json) => StudentDto.fromJson(json);

  Future<PagedResult<StudentEnrollmentDto>> getEnrollments(
    int studentId, {
    int page = 1,
    int pageSize = 20,
  }) async {
    final params = pagedQuery(page, pageSize, '');
    final response = await apiClient.get(
      '$endpoint/$studentId/enrollments',
      queryParams: params,
    );
    return parsePage<StudentEnrollmentDto>(
      response,
      StudentEnrollmentDto.fromJson,
      params: params,
    );
  }
}
