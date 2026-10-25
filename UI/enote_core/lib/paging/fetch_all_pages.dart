import 'paged_query.dart';
import 'paged_result.dart';

const int maxPageSize = 100;

Future<List<T>> fetchAllPages<T>(
  Future<PagedResult<T>> Function(Map<String, dynamic> query) search, {
  Map<String, dynamic>? filters,
}) async {
  final items = <T>[];
  var page = 1;

  while (true) {
    final result = await search(
      pagedQuery(page, maxPageSize, '', filters: filters),
    );
    items.addAll(result.items);

    if (!result.hasNext || result.items.isEmpty) break;
    page++;
  }

  return items;
}
