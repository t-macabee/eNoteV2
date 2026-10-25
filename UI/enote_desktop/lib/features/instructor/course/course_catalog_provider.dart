import 'package:enote_core/enote_core.dart';

class CourseCatalogProvider extends ReadOnlyProvider<CourseDto> {
  CourseCatalogProvider({
    required super.apiClient,
  }) : super(endpoint: 'instructor/courses/catalog');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);

  Future<PagedResult<CourseCatalogInstructorDto>> getCatalogInstructors({
    Map<String, dynamic>? params,
  }) async {
    final response = await apiClient.get(
      '$endpoint/instructors',
      queryParams: params,
    );
    throwIfError(response);
    return parsePage<CourseCatalogInstructorDto>(
      response,
      (json) => CourseCatalogInstructorDto.fromJson(json),
      params: params,
    );
  }

  Future<CourseCatalogSummaryDto> getCatalogSummary() async {
    final response = await apiClient.get('$endpoint/summary');
    final data = decodeOrThrow(response);
    return CourseCatalogSummaryDto.fromJson(data);
  }
}
