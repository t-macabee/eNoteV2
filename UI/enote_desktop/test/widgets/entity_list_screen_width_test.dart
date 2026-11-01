import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

void main() {
  EntityListConfig<String> config({
    EntityListPresentation presentation = EntityListPresentation.embedded,
  }) => EntityListConfig<String>(
    presentation: presentation,
    columns: [ColumnSpec<String>(label: 'Naziv', value: (item) => item)],
    fetcher: (page, pageSize, search) async => PagedResult<String>(
      items: const ['Sarajevo', 'Mostar'],
      page: page,
      pageSize: pageSize,
      totalCount: 2,
    ),
  );

  Future<void> setViewport(WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
  }

  testWidgets('embedded list limits its content to the content max width', (
    tester,
  ) async {
    await setViewport(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: EntityListScreen<String>(config: config())),
      ),
    );
    await tester.pumpAndSettle();

    final toolbar = tester.getRect(find.byType(EntityToolbar));
    final centeredLeft = (1280 - AppSize.contentMaxWidth) / 2;

    expect(toolbar.width, lessThanOrEqualTo(AppSize.contentMaxWidth));
    expect(toolbar.left, centeredLeft);
    expect(toolbar.right, 1280 - centeredLeft);
  });

  testWidgets('table fills the same width as the toolbar and the footer', (
    tester,
  ) async {
    await setViewport(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: EntityListScreen<String>(
          config: config(presentation: EntityListPresentation.page),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final toolbar = tester.getRect(find.byType(EntityToolbar));
    final table = tester.getRect(find.byType(DataTable));
    final footer = tester.getRect(
      find
          .ancestor(
            of: find.byType(PagedPaginationBar),
            matching: find.byType(Container),
          )
          .first,
    );

    expect(table.width, greaterThan(0));
    expect(table.left, toolbar.left);
    expect(table.right, toolbar.right);
    expect(footer.width, toolbar.width);
    expect(footer.left, toolbar.left);
    expect(footer.right, toolbar.right);
  });

  testWidgets('table column spacing uses the spacing token', (tester) async {
    await setViewport(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: EntityListScreen<String>(config: config())),
      ),
    );
    await tester.pumpAndSettle();

    final table = tester.widget<DataTable>(find.byType(DataTable));

    expect(table.columnSpacing, AppSpacing.xl);
  });

  testWidgets('embedded list in a dialog still shrinks to its content', (
    tester,
  ) async {
    await setViewport(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Center(
            child: Dialog(
              child: ConstrainedBox(
                constraints: const BoxConstraints(
                  maxWidth: 700,
                  maxHeight: 700,
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Flexible(child: EntityListScreen<String>(config: config())),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Sarajevo'), findsOneWidget);

    final list = tester.getRect(find.byType(EntityListScreen<String>));
    final dialogBody = tester.getRect(
      find
          .descendant(of: find.byType(Dialog), matching: find.byType(Column))
          .first,
    );

    expect(list.width, lessThanOrEqualTo(700));
    expect(list.height, lessThan(700));
    expect(dialogBody.height, list.height);
  });
}
