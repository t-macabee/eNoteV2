import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';
import 'package:enote_desktop/widgets/entity_list_sections.dart';

void main() {
  testWidgets('EntityListTable scrolls horizontally for a wide row',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(800, 600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final longText = 'A' * 400;
    final config = EntityListConfig<String>(
      columns: [
        ColumnSpec<String>(label: 'Naziv', value: (item) => item),
      ],
      fetcher: (page, pageSize, search) async => PagedResult<String>(
        items: const <String>[],
        page: page,
        pageSize: pageSize,
        totalCount: 0,
      ),
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: EntityListTable<String>(
            config: config,
            items: [longText],
            onDelete: (_) {},
          ),
        ),
      ),
    );

    expect(tester.takeException(), isNull);
    expect(find.text(longText), findsOneWidget);
  });
}
