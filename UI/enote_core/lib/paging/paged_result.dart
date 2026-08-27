class PagedResult<T> {
  final List<T> items;
  final int page;
  final int pageSize;
  final int? totalCount;

  PagedResult({
    required this.items,
    this.page = 1,
    this.pageSize = 20,
    this.totalCount,
  });

  int get count => items.length;
  int? get totalPages =>
      totalCount != null && pageSize > 0
          ? (totalCount! / pageSize).ceil()
          : null;
  bool get hasNext =>
      totalCount != null ? (page * pageSize) < totalCount! : items.length == pageSize;
  bool get hasPrevious => page > 1;

  factory PagedResult.fromJson(
    List<dynamic> jsonItems,
    T Function(Map<String, dynamic>) fromJson, {
    int page = 1,
    int pageSize = 20,
    int? totalCount,
  }) {
    return PagedResult<T>(
      items: jsonItems.map((e) => fromJson(Map<String, dynamic>.from(e))).toList(),
      page: page,
      pageSize: pageSize,
      totalCount: totalCount,
    );
  }
}
