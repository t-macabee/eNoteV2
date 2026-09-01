import 'package:enote_core/enote_core.dart';

/// Admin-scoped course provider (admin/courses).
///
/// Admins can create a course on an instructor's behalf (via [insert],
/// inherited from [BaseProvider]) — update/delete is still deliberately
/// absent here; course edit/delete stays Instructor-owned.
class AdminCourseProvider extends BaseProvider<CourseDto> {
  AdminCourseProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);
}
