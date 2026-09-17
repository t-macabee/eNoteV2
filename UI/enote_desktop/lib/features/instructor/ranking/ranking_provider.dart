import 'package:enote_core/enote_core.dart';

class RankingProvider {
  final ApiClient apiClient;

  RankingProvider({required this.apiClient});

  Future<List<CourseRankingEntryDto>> getForCourse(int courseId) async {
    final response = await apiClient.get(
      'instructor/courses/$courseId/ranking',
    );
    throwIfError(response);
    return parseCourseRanking(response.body);
  }

  String reportEndpoint(int courseId) =>
      'instructor/courses/$courseId/ranking/report';
}
