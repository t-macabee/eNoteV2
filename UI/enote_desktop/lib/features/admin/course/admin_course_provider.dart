import 'package:enote_core/enote_core.dart';

/// Admin-scoped course provider (admin/courses).
///
/// Read-only in practice: `AdminCourseController` only exposes `GetPaged`/
/// `GetById` for cross-system oversight. Course create/edit/delete is
/// Instructor-owned (`CourseController`) — nothing here calls insert/update/remove.
class AdminCourseProvider extends ReadOnlyProvider<CourseDto> {
  AdminCourseProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);
}
