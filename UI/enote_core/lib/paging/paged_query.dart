Map<String, dynamic> pagedQuery(
  int page,
  int pageSize,
  String search, {
  String? searchField,
  Map<String, dynamic>? filters,
}) {
  final query = <String, dynamic>{
    'page': page,
    'pageSize': pageSize,
    'includeTotalCount': true,
  };

  if (searchField != null && search.isNotEmpty) {
    query[searchField] = search;
  }

  if (filters != null) {
    query.addAll(filters);
  }

  return query;
}
