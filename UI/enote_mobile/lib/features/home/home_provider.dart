import 'package:enote_core/enote_core.dart';

class HomeProvider {
  final ApiClient apiClient;

  HomeProvider({required this.apiClient});

  Future<StudentHomeDto> load() async {
    final response = await apiClient.get('student/home');
    return StudentHomeDto.fromJson(decodeOrThrow(response));
  }
}
