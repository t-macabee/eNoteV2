import 'package:enote_core/enote_core.dart';

class InstructorStudentProvider extends BaseProvider<StudentDto> {
  InstructorStudentProvider({required super.apiClient})
      : super(endpoint: 'instructor/students');

  @override
  StudentDto fromJson(Map<String, dynamic> json) => StudentDto.fromJson(json);

  Future<List<StudentEnrollmentDto>> getEnrollments(int studentId) async {
    final response = await apiClient.get('$endpoint/$studentId/enrollments');
    return decodeList(response, StudentEnrollmentDto.fromJson);
  }
}
