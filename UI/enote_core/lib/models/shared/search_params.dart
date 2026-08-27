class SearchParams {
  final int page;
  final int pageSize;
  final bool includeTotalCount;

  SearchParams({
    this.page = 1,
    this.pageSize = 20,
    this.includeTotalCount = true,
  });

  Map<String, dynamic> toQueryMap() {
    final map = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
      'includeTotalCount': includeTotalCount,
    };
    return map;
  }

  SearchParams copyWith({
    int? page,
    int? pageSize,
    bool? includeTotalCount,
  }) {
    return SearchParams(
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
      includeTotalCount: includeTotalCount ?? this.includeTotalCount,
    );
  }
}
