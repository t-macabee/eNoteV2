import 'dart:convert';

import 'package:enote_core/enote_core.dart';

class CourseCatalogProvider extends ReadOnlyProvider<CourseDto> {
  CourseCatalogProvider({
    required super.apiClient,
  }) : super(endpoint: 'instructor/courses/catalog');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);

  Future<List<CourseCatalogInstructorDto>> getCatalogInstructors() async {
    final response = await apiClient.get('$endpoint/instructors');
    throwIfError(response);
    final list = jsonDecode(response.body) as List<dynamic>;
    return list
        .map((e) => CourseCatalogInstructorDto.fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }

  Future<CourseCatalogSummaryDto> getCatalogSummary() async {
    final response = await apiClient.get('$endpoint/summary');
    final data = decodeOrThrow(response);
    return CourseCatalogSummaryDto.fromJson(data);
  }
}
