import 'package:enote_core/enote_core.dart';

/// Course ranking over `student/courses/{courseId}/ranking` (01 §4.3).
///
/// Plain class (no listeners): the ranking screen loads once per open.
class RankingProvider {
  final ApiClient apiClient;

  RankingProvider({required this.apiClient});

  /// The server returns the top 15 (plus the caller's own row when it is below 15) as a bare JSON list.
  Future<List<CourseRankingEntryDto>> getForCourse(int courseId) async {
    final response = await apiClient.get(
      'student/courses/$courseId/ranking',
    );
    throwIfError(response);
    return parseCourseRanking(response.body);
  }
}
