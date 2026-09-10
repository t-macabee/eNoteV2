import 'package:enote_core/enote_core.dart';

/// Student assignments over `student/assignments` (01 §4.3, §6.1).
class AssignmentProvider extends ReadOnlyProvider<AssignmentDto> {
  AssignmentProvider({required super.apiClient})
    : super(endpoint: 'student/assignments');

  @override
  AssignmentDto fromJson(Map<String, dynamic> json) =>
      AssignmentDto.fromJson(json);

  /// `GET student/assignments/{id}/submission`.
  ///
  /// A 404 means nothing has been submitted yet, which is a normal state
  /// (state A / A′) rather than an error (01 §6.4 revised, gap A3 closed).
  Future<AssignmentSubmissionDto?> mySubmission(int id) async {
    final response = await apiClient.get('$endpoint/$id/submission');
    if (response.statusCode == 404) return null;
    return AssignmentSubmissionDto.fromJson(decodeOrThrow(response));
  }

  /// `POST student/assignments/{id}/submit` (multipart part `file`) → 200
  /// with the created submission.
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

  /// `GET student/submissions` — the student's own submission history,
  /// newest first by server order (`SubmittedAt` desc, then `Id` desc).
  /// Takes page fields only (01 G11 revised).
  Future<PagedResult<AssignmentSubmissionDto>> myHistory({
    int page = 1,
    int pageSize = 20,
  }) async {
    final params = {
      'page': page,
      'pageSize': pageSize,
      'includeTotalCount': true,
    };
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
