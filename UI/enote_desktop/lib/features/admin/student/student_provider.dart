import 'package:enote_core/enote_core.dart';

class StudentProvider extends BaseProvider<StudentDto> {
  StudentProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/students');

  @override
  StudentDto fromJson(Map<String, dynamic> json) =>
      StudentDto.fromJson(json);
}
