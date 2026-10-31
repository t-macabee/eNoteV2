import 'package:enote_core/enote_core.dart';

class StudentDirectoryProvider {
  final ApiClient apiClient;
  final String endpoint = 'instructor/student-directory';

  StudentDirectoryProvider({required this.apiClient});

  Future<PagedResult<StudentDirectoryEntryDto>> search(
    Map<String, dynamic> params,
  ) async {
    final response = await apiClient.get(endpoint, queryParams: params);
    return decodePage<StudentDirectoryEntryDto>(
      response,
      StudentDirectoryEntryDto.fromJson,
      params: params,
    );
  }

  Future<StudentProfileDto> getProfile(int id) async {
    final response = await apiClient.get('$endpoint/$id');
    final data = decodeOrThrow(response);
    return StudentProfileDto.fromJson(data);
  }
}
