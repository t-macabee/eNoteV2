import 'package:flutter/foundation.dart';
import 'package:enote_core/enote_core.dart';

class InstrumentProvider extends ReadOnlyProvider<InstrumentDto> {
  static const int pageSize = 20;

  InstrumentProvider({required super.apiClient})
    : super(endpoint: 'instruments/public');

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);

  static const String _queryPrefix = 'search.';

  Future<PagedResult<InstrumentDto>> fetchPage(
    int page,
    int pageSize,
    String search, {
    bool onlyAvailable = true,
  }) {
    return getPage(
      params: {
        '${_queryPrefix}Page': page,
        '${_queryPrefix}PageSize': pageSize,
        '${_queryPrefix}IncludeTotalCount': true,
        if (search.isNotEmpty) '${_queryPrefix}Search': search,
        if (onlyAvailable) '${_queryPrefix}IsAvailable': true,
      },
    );
  }

  Future<List<InstrumentRecommendationDto>> recommended({int count = 5}) async {
    final response = await apiClient.get(
      'student/instruments/recommended',
      queryParams: {'count': count},
    );
    return decodeList(response, InstrumentRecommendationDto.fromJson);
  }

  Future<void> recordView(int id) async {
    try {
      final response = await apiClient.post('student/instruments/$id/view');
      throwIfError(response);
    } catch (e) {
      debugPrint('Recording the instrument view failed: $e');
    }
  }
}
