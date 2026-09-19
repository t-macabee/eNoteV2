import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/widgets/paged_list_view.dart';

import '../helpers.dart';

PagedFetchController<int> _controller(
  Future<PagedResult<int>> Function(int page, int pageSize, String search)
  fetcher,
) {
  return PagedFetchController<int>(fetcher: fetcher, pageSize: 20);
}

Future<void> _pumpList(
  WidgetTester tester,
  PagedFetchController<int> controller,
) async {
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: PagedListView<int>(
          controller: controller,
          itemBuilder: (context, item) => ListTile(title: Text('Item $item')),
        ),
      ),
    ),
  );
  await tester.pump();
  await tester.pump();
}

void main() {
  testWidgets('search debounce resets to page 1', (tester) async {
    final calls = <Map<String, Object?>>[];
    final controller = _controller((page, pageSize, search) async {
      calls.add({'page': page, 'search': search});
      return PagedResult<int>(
        items: const [],
        page: page,
        pageSize: pageSize,
        totalCount: 0,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    calls.clear();

    await tester.enterText(find.byType(TextField), 'abc');
    await tester.pump();
    expect(calls, isEmpty);
    await pumpPastDebounce(tester);
    await tester.pump();
    expect(calls.length, 1);
    expect(calls.first['page'], 1);
    expect(calls.first['search'], 'abc');
  });

  testWidgets('page bar moves between pages', (tester) async {
    final controller = _controller((page, pageSize, search) async {
      return PagedResult<int>(
        items: [page],
        page: page,
        pageSize: pageSize,
        totalCount: 60,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);

    expect(find.text('Stranica 1 od 3'), findsOneWidget);
    expect(find.text('Item 1'), findsOneWidget);
    await tester.tap(find.text('Sljedeća'));
    await tester.pump();
    await tester.pump();
    expect(find.text('Stranica 2 od 3'), findsOneWidget);
    expect(find.text('Item 2'), findsOneWidget);
    await tester.tap(find.text('Prethodna'));
    await tester.pump();
    await tester.pump();
    expect(find.text('Stranica 1 od 3'), findsOneWidget);
  });

  testWidgets('empty state shows the default copy', (tester) async {
    final controller = _controller((page, pageSize, search) async {
      return PagedResult<int>(
        items: const [],
        page: page,
        pageSize: pageSize,
        totalCount: 0,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    expect(find.text('Nema rezultata za pretragu.'), findsOneWidget);
  });

  testWidgets('a failed load shows retry and the retry refetches', (
    tester,
  ) async {
    var calls = 0;
    final controller = _controller((page, pageSize, search) async {
      calls++;
      throw Exception('boom');
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);
    expect(find.text('Pokušaj ponovo'), findsOneWidget);

    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pump();
    await tester.pump();
    expect(calls, 2);
  });

  testWidgets('an error on top of loaded rows keeps the rows visible', (
    tester,
  ) async {
    var fail = false;
    final controller = _controller((page, pageSize, search) async {
      if (fail) throw Exception('boom');
      return PagedResult<int>(
        items: const [1, 2],
        page: page,
        pageSize: pageSize,
        totalCount: 2,
      );
    });
    addTearDown(controller.dispose);
    await _pumpList(tester, controller);

    fail = true;
    controller.load();
    await tester.pump();
    await tester.pump();

    expect(find.text('Item 1'), findsOneWidget);
    expect(find.text('Item 2'), findsOneWidget);
    expect(find.text('Pokušaj ponovo'), findsNothing);
  });
}
