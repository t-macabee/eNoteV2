import 'dart:convert';

import 'package:enote_core/enote_core.dart';

/// Catalogue provider for the Instrumenti tab (S7, S8).
///
/// The list endpoint is anonymous (`instruments/public`); the recommendation
/// and view-tracking endpoints are student-scoped.
class InstrumentProvider extends ReadOnlyProvider<InstrumentDto> {
  /// Rows per catalogue page (02 §3).
  static const int pageSize = 20;

  InstrumentProvider({required super.apiClient})
    : super(endpoint: 'instruments/public');

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);

  /// Query-string prefix every parameter of this endpoint must carry.
  ///
  /// `Observed fact` (verified against the running API 2026-09-09): the
  /// action binds `[FromQuery] InstrumentSearchObject search`, so the moment
  /// the query string contains a key literally named `search`, ASP.NET model
  /// binding switches to the `search.` prefix and silently drops every
  /// unprefixed key — `page`, `pageSize`, `includeTotalCount`, `isAvailable`
  /// **and** the search term itself. `?search=Fender` therefore returns all
  /// 25 instruments on a default-sized page. Sending the whole query
  /// prefixed binds correctly and is the only client-side form that works.
  static const String _queryPrefix = 'search.';

  /// One catalogue page. [onlyAvailable] maps to the server's `IsAvailable`
  /// filter and is omitted when off, so the toggle off means "show all"
  /// rather than "show only unavailable".
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

  /// `GET student/instruments/recommended?count=` (C1).
  Future<List<InstrumentRecommendationDto>> recommended({int count = 5}) async {
    final response = await apiClient.get(
      'student/instruments/recommended',
      queryParams: {'count': count},
    );
    throwIfError(response);
    final decoded = jsonDecode(response.body);
    if (decoded is! List) return const [];
    return decoded
        .map(
          (e) => InstrumentRecommendationDto.fromJson(
            Map<String, dynamic>.from(e as Map),
          ),
        )
        .toList();
  }

  /// `POST student/instruments/{id}/view`.
  ///
  /// An analytics signal only: every failure is swallowed so the detail
  /// screen never blocks or errors on it (01 §4.3).
  Future<void> recordView(int id) async {
    try {
      await apiClient.post('student/instruments/$id/view');
    } catch (_) {
      // Deliberately ignored.
    }
  }
}
