import 'package:enote_core/enote_core.dart';

class InstructorEnrollmentProvider
    extends ReadOnlyProvider<CourseEnrollmentDto> {
  InstructorEnrollmentProvider({required super.apiClient})
    : super(endpoint: 'instructor/enrollments');

  @override
  CourseEnrollmentDto fromJson(Map<String, dynamic> json) =>
      CourseEnrollmentDto.fromJson(json);

  Future<PagedResult<CourseEnrollmentDto>> getForCourse(
    int courseId, {
    Map<String, dynamic>? params,
  }) async {
    final response = await apiClient.get(
      'instructor/courses/$courseId/enrollments',
      queryParams: params,
    );
    throwIfError(response);
    return parsePage<CourseEnrollmentDto>(
      response,
      (json) => CourseEnrollmentDto.fromJson(json),
      params: params,
    );
  }

  Future<int?> countPending({int? courseId}) async {
    final page = await getPage(
      params: pagedQuery(
        1,
        1,
        '',
        filters: {
          'enrollmentStatus': EnrollmentStatus.pending.toJson(),
          'courseId': ?courseId,
        },
      ),
    );
    return page.totalCount;
  }

  Future<CourseEnrollmentDto> approve(CourseEnrollmentDto enrollment) =>
      _decide(enrollment, 'approve');

  Future<CourseEnrollmentDto> reject(
    CourseEnrollmentDto enrollment,
    String reason,
  ) => _decide(enrollment, 'reject', body: {'reason': reason});

  Future<CourseEnrollmentDto> complete(CourseEnrollmentDto enrollment) =>
      _decide(enrollment, 'complete');

  Future<CourseEnrollmentDto> _decide(
    CourseEnrollmentDto enrollment,
    String action, {
    Map<String, dynamic>? body,
  }) async {
    final response = await apiClient.post(
      'instructor/courses/${enrollment.courseId}/enrollments/${enrollment.id}/$action',
      body: body,
    );
    final data = decodeOrThrow(response);
    return CourseEnrollmentDto.fromJson(data);
  }
}
