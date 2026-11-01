import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_config.dart';
import 'package:enote_desktop/widgets/entity_list_sections.dart';

void main() {
  testWidgets('rowColor tints the matching row only', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    const tint = Color(0x26EF4444);
    final config = EntityListConfig<String>(
      columns: [ColumnSpec<String>(label: 'Naziv', value: (item) => item)],
      fetcher: (page, pageSize, search) async => PagedResult<String>(
        items: const <String>[],
        page: page,
        pageSize: pageSize,
        totalCount: 0,
      ),
      rowColor: (item) => item == 'Sarajevo' ? tint : null,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: EntityListTable<String>(
            config: config,
            items: const ['Sarajevo', 'Mostar'],
            onDelete: (_) {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final rows = tester.widget<Table>(find.byType(Table)).children;
    expect(rows, hasLength(3));

    final tinted = rows[1].decoration! as BoxDecoration;
    final plain = rows[2].decoration! as BoxDecoration;

    expect(tinted.color, tint);
    expect(plain.color, isNot(tint));
  });
}
