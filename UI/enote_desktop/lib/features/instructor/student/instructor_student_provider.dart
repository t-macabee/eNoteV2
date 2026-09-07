import 'package:enote_core/enote_core.dart';

class InstructorStudentProvider extends BaseProvider<StudentDto> {
  InstructorStudentProvider({required super.apiClient})
      : super(endpoint: 'instructor/students');

  @override
  StudentDto fromJson(Map<String, dynamic> json) => StudentDto.fromJson(json);

}
