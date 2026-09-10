import 'package:enote_core/enote_core.dart';

/// Student course catalogue over `student/courses` (01 §4.3).
class CourseProvider extends ReadOnlyProvider<CourseDto> {
  CourseProvider({required super.apiClient})
    : super(endpoint: 'student/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);

  /// POST `student/courses/{id}/enroll` (204, no body).
  Future<void> enroll(int id) async {
    final response = await apiClient.post('$endpoint/$id/enroll');
    throwIfError(response);
    notifyListeners();
  }

  /// POST `student/courses/{id}/unenroll` (204, no body).
  Future<void> unenroll(int id) async {
    final response = await apiClient.post('$endpoint/$id/unenroll');
    throwIfError(response);
    notifyListeners();
  }
}
