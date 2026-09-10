import 'dart:convert';

import 'package:enote_core/enote_core.dart';

/// Course ranking over `student/courses/{courseId}/ranking` (01 §4.3).
///
/// Plain class (no listeners): the ranking screen loads once per open.
class RankingProvider {
  final ApiClient apiClient;

  RankingProvider({required this.apiClient});

  /// The server returns the ranking unpaged (02 D6) — a bare JSON list.
  /// A `{items: [...]}` envelope is accepted too.
  Future<List<CourseRankingEntryDto>> getForCourse(int courseId) async {
    final response = await apiClient.get(
      'student/courses/$courseId/ranking',
    );
    throwIfError(response);
    if (response.body.trim().isEmpty) return [];
    final decoded = jsonDecode(response.body);
    final List<dynamic> items;
    if (decoded is List) {
      items = decoded;
    } else if (decoded is Map<String, dynamic>) {
      // `Observed fact` from the live API: the endpoint wraps the unpaged
      // list as `{"value": [...], "Count": n}`.
      items =
          decoded['items'] as List<dynamic>? ??
          decoded['value'] as List<dynamic>? ??
          [];
    } else {
      return [];
    }
    return items
        .map(
          (e) => CourseRankingEntryDto.fromJson(
            Map<String, dynamic>.from(e as Map),
          ),
        )
        .toList();
  }
}
