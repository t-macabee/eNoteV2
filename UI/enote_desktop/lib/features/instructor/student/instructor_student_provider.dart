import 'package:enote_core/enote_core.dart';

class InstructorStudentProvider extends BaseProvider<StudentDto> {
  InstructorStudentProvider({required super.apiClient})
      : super(endpoint: 'instructor/students');

  @override
  StudentDto fromJson(Map<String, dynamic> json) => StudentDto.fromJson(json);

  Future<int> createStudent(DelegatedUserCreateRequest request) async {
    final response = await apiClient.post(
      'instructor/students',
      body: request.toJson(),
    );
    final data = response as Map<String, dynamic>;
    return data['userId'] as int? ?? 0;
  }
}
