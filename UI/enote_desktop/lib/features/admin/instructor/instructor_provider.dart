import 'package:enote_core/enote_core.dart';

class InstructorProvider extends BaseProvider<InstructorDto> {
  InstructorProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/instructors');

  @override
  InstructorDto fromJson(Map<String, dynamic> json) =>
      InstructorDto.fromJson(json);
}
