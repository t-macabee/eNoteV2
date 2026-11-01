import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

void main() {
  const city = 'Sarajevo';
  const longName = 'Muzička škola Bosanski Petrovac Centar za talente';

  EntityListConfig<String> config({
    List<String> items = const [city, 'Mostar'],
    void Function(BuildContext, String)? onRowTap,
    void Function(BuildContext, String)? onEdit,
    Future<bool?> Function(BuildContext, String)? onDelete,
    double? nameMaxWidth,
  }) => EntityListConfig<String>(
    presentation: EntityListPresentation.embedded,
    columns: [
      ColumnSpec<String>(
        label: 'Naziv',
        value: (item) => item,
        maxWidth: nameMaxWidth,
      ),
    ],
    fetcher: (page, pageSize, search) async => PagedResult<String>(
      items: items,
      page: page,
      pageSize: pageSize,
      totalCount: items.length,
    ),
    onRowTap: onRowTap,
    onEdit: onEdit,
    onDelete: onDelete,
  );

  Future<void> pumpList(
    WidgetTester tester,
    EntityListConfig<String> listConfig,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: EntityListScreen<String>(config: listConfig)),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('table row tap calls onRowTap when it is set', (tester) async {
    String? tapped;
    var editCalls = 0;

    await pumpList(
      tester,
      config(
        onRowTap: (_, item) => tapped = item,
        onEdit: (_, _) => editCalls++,
      ),
    );

    await tester.tap(find.text(city));
    await tester.pumpAndSettle();

    expect(tapped, city);
    expect(editCalls, 0);
  });

  testWidgets('table row tap falls back to onEdit', (tester) async {
    String? edited;

    await pumpList(tester, config(onEdit: (_, item) => edited = item));

    await tester.tap(find.text(city));
    await tester.pumpAndSettle();

    expect(edited, city);
  });

  testWidgets('maxWidth column ellipsizes long text and shows a tooltip', (
    tester,
  ) async {
    await pumpList(tester, config(items: const [longName], nameMaxWidth: 120));

    final text = tester.widget<Text>(find.text(longName));
    expect(text.maxLines, 1);
    expect(text.overflow, TextOverflow.ellipsis);
    expect(tester.getSize(find.text(longName)).width, lessThanOrEqualTo(120));
    expect(find.byTooltip(longName), findsOneWidget);
  });

  testWidgets('edit and delete row actions have tooltips', (tester) async {
    await pumpList(
      tester,
      config(onEdit: (_, _) {}, onDelete: (_, _) async => true),
    );

    expect(find.byTooltip('Uredi'), findsWidgets);
    expect(find.byTooltip('Obriši'), findsWidgets);
  });
}
