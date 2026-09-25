import 'package:enote_core/enote_core.dart';

class CourseProvider extends CrudProvider<CourseDto> {
  CourseProvider({
    required super.apiClient,
  }) : super(endpoint: 'instructor/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);

  Future<PagedResult<CourseEnrollmentDto>> getEnrollments(
    int courseId, {
    Map<String, dynamic>? params,
  }) async {
    final response = await apiClient.get(
      '$endpoint/$courseId/enrollments',
      queryParams: params,
    );
    throwIfError(response);
    return parsePage<CourseEnrollmentDto>(
      response,
      (json) => CourseEnrollmentDto.fromJson(json),
      params: params,
    );
  }

  Future<CourseEnrollmentDto> approveEnrollment(
    int courseId,
    int enrollmentId,
  ) async {
    final response = await apiClient.post(
      '$endpoint/$courseId/enrollments/$enrollmentId/approve',
    );
    final data = decodeOrThrow(response);
    return CourseEnrollmentDto.fromJson(data);
  }

  Future<CourseEnrollmentDto> rejectEnrollment(
    int courseId,
    int enrollmentId,
    String reason,
  ) async {
    final response = await apiClient.post(
      '$endpoint/$courseId/enrollments/$enrollmentId/reject',
      body: {'reason': reason},
    );
    final data = decodeOrThrow(response);
    return CourseEnrollmentDto.fromJson(data);
  }

  Future<CourseEnrollmentDto> completeEnrollment(
    int courseId,
    int enrollmentId,
  ) async {
    final response = await apiClient.post(
      '$endpoint/$courseId/enrollments/$enrollmentId/complete',
    );
    final data = decodeOrThrow(response);
    return CourseEnrollmentDto.fromJson(data);
  }
}
