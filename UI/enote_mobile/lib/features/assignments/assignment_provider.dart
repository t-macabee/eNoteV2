import 'package:enote_core/enote_core.dart';

class AssignmentProvider extends ReadOnlyProvider<AssignmentDto> {
  AssignmentProvider({required super.apiClient})
    : super(endpoint: 'student/assignments');

  @override
  AssignmentDto fromJson(Map<String, dynamic> json) =>
      AssignmentDto.fromJson(json);

  Future<AssignmentSubmissionDto?> mySubmission(int id) async {
    final response = await apiClient.get('$endpoint/$id/submission');
    if (response.statusCode == 404) return null;
    return AssignmentSubmissionDto.fromJson(decodeOrThrow(response));
  }

  Future<AssignmentSubmissionDto> submit(
    int id,
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    final response = await apiClient.postMultipart(
      '$endpoint/$id/submit',
      bytes: bytes,
      fileName: fileName,
      contentType: contentType,
    );
    return AssignmentSubmissionDto.fromJson(decodeOrThrow(response));
  }

  Future<PagedResult<AssignmentSubmissionDto>> myHistory({
    int page = 1,
    int pageSize = 20,
  }) async {
    final params = pagedQuery(page, pageSize, '');
    final response = await apiClient.get(
      'student/submissions',
      queryParams: params,
    );
    throwIfError(response);
    return parsePage<AssignmentSubmissionDto>(
      response,
      AssignmentSubmissionDto.fromJson,
      params: params,
    );
  }
}
