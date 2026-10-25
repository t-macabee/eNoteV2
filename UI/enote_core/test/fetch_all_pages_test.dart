import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  test('requests every page until hasNext is false', () async {
    final requestedPages = <int>[];

    final items = await fetchAllPages<int>((query) async {
      final page = query['page'] as int;
      requestedPages.add(page);
      final isLastPage = page == 3;

      return PagedResult<int>(
        items: [
          for (var i = 0; i < (isLastPage ? 50 : maxPageSize); i++)
            page * 1000 + i,
        ],
        page: page,
        pageSize: query['pageSize'] as int,
        totalCount: 250,
      );
    });

    expect(requestedPages, [1, 2, 3]);
    expect(items.length, 250);
    expect(items.first, 1000);
    expect(items.last, 3049);
  });

  test('stops after one request when hasNext is false', () async {
    var calls = 0;

    final items = await fetchAllPages<int>((query) async {
      calls++;
      return PagedResult<int>(
        items: const [1, 2],
        page: 1,
        pageSize: maxPageSize,
        totalCount: 2,
      );
    });

    expect(calls, 1);
    expect(items, [1, 2]);
  });

  test('stops on an empty page even when hasNext is true', () async {
    final requestedPages = <int>[];

    final items = await fetchAllPages<int>((query) async {
      final page = query['page'] as int;
      requestedPages.add(page);
      return PagedResult<int>(
        items: page == 1 ? const [1] : const [],
        page: page,
        pageSize: maxPageSize,
        totalCount: 500,
      );
    });

    expect(requestedPages, [1, 2]);
    expect(items, [1]);
  });

  test('sends the page size and the filters on every request', () async {
    final queries = <Map<String, dynamic>>[];

    await fetchAllPages<int>(
      (query) async {
        queries.add(query);
        return PagedResult<int>(
          items: const [1],
          page: 1,
          pageSize: maxPageSize,
          totalCount: 1,
        );
      },
      filters: {'studentId': 5},
    );

    expect(queries, hasLength(1));
    expect(queries.single['page'], 1);
    expect(queries.single['pageSize'], maxPageSize);
    expect(queries.single['includeTotalCount'], isTrue);
    expect(queries.single['studentId'], 5);
    expect(maxPageSize, 100);
  });
}
