import 'package:enote_core/enote_core.dart';

/// Admin-scoped course provider (admin/courses).
///
/// Admins can create a course on an instructor's behalf (via [insert]) and
/// soft-delete any course regardless of owning instructor (via [remove]),
/// both inherited from [BaseProvider] and backed by admin/courses endpoints.
/// Update is still deliberately absent here; course edit stays Instructor-owned.
class AdminCourseProvider extends BaseProvider<CourseDto> {
  AdminCourseProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);
}
