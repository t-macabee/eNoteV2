import 'package:enote_core/enote_core.dart';

class CourseProvider extends BaseProvider<CourseDto> {
  CourseProvider({
    required super.apiClient,
  }) : super(endpoint: 'instructor/courses');

  @override
  CourseDto fromJson(Map<String, dynamic> json) => CourseDto.fromJson(json);
}
