import 'package:enote_core/enote_core.dart';

/// Read-only admin-scoped course provider (admin/courses).
///
/// Course ownership stays with instructors; admins get paged list + get-by-id
/// oversight only — deliberately no create/update/delete here.
class AdminCourseProvider extends BaseProvider<CourseDto> {
  AdminCourseProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);
}
