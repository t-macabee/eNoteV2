import '../api/api_exception.dart';
import 'paged_query.dart';
import 'paged_result.dart';

const int maxPageSize = 100;
const int defaultMaxPages = 10;

Future<List<T>> fetchAllPages<T>(
  Future<PagedResult<T>> Function(Map<String, dynamic> query) search, {
  Map<String, dynamic>? filters,
  int maxPages = defaultMaxPages,
}) async {
  final items = <T>[];
  var page = 1;

  while (page <= maxPages) {
    final result = await search(
      pagedQuery(page, maxPageSize, '', filters: filters),
    );
    items.addAll(result.items);

    if (!result.hasNext || result.items.isEmpty) return items;
    page++;
  }

  throw ApiException(
    'Previše stranica za učitavanje. Sužite pretragu i pokušajte ponovo.',
  );
}
