import 'package:enote_core/enote_core.dart';

class CourseProvider extends ReadOnlyProvider<CourseDto> {
  CourseProvider({required super.apiClient})
    : super(endpoint: 'student/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);

  Future<void> enroll(int id) async {
    final response = await apiClient.post('$endpoint/$id/enroll');
    throwIfError(response);
    notifyListeners();
  }

  Future<void> unenroll(int id) async {
    final response = await apiClient.post('$endpoint/$id/unenroll');
    throwIfError(response);
    notifyListeners();
  }
}
