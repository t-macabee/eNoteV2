import 'package:enote_core/enote_core.dart';

enum AssignmentFilter {
  open,
  submitted,
  pastDue,
  all;

  Map<String, dynamic> query(DateTime now) => switch (this) {
    AssignmentFilter.open => {'isSubmitted': false, 'dueAfter': toUtcIso(now)},
    AssignmentFilter.submitted => {'isSubmitted': true},
    AssignmentFilter.pastDue => {
      'isSubmitted': false,
      'dueBefore': toUtcIso(now),
    },
    AssignmentFilter.all => const <String, dynamic>{},
  };
}

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
    final submission = AssignmentSubmissionDto.fromJson(
      decodeOrThrow(response),
    );
    notifyListeners();
    return submission;
  }

  Future<int?> countOpen() async {
    final result = await getPage(
      params: pagedQuery(
        1,
        1,
        '',
        filters: AssignmentFilter.open.query(DateTime.now()),
      ),
    );
    return result.totalCount;
  }
}
