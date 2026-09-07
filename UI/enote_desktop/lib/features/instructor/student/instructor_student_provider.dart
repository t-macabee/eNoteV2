import 'package:enote_core/enote_core.dart';

class InstructorStudentProvider extends BaseProvider<StudentDto> {
  InstructorStudentProvider({required super.apiClient})
      : super(endpoint: 'instructor/students');

  @override
  StudentDto fromJson(Map<String, dynamic> json) => StudentDto.fromJson(json);

  Future<int> createStudent(DelegatedUserCreateRequest request) async {
    final data = decodeOrThrow(await apiClient.post(
      'instructor/students',
      body: request.toJson(),
    ));
    return data['userId'] as int? ?? 0;
  }
}
