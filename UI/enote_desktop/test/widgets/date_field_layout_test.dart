import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/date_field.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  group('DateField layout', () {
    testWidgets('empty and filled DateField have the same height', (
      tester,
    ) async {
      _useDesktopView(tester);

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Column(
              children: [
                SizedBox(width: 180, child: DateField(labelText: 'Od')),
                SizedBox(
                  width: 180,
                  child: DateField(
                    labelText: 'Do',
                    initialValue: DateTime(2026, 11, 1),
                  ),
                ),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final emptyField = find.byType(DateField).at(0);
      final filledField = find.byType(DateField).at(1);

      expect(
        tester.getSize(filledField).height,
        tester.getSize(emptyField).height,
      );
    });

    testWidgets('TextField and DateField in a centered Row have the same '
        'height', (tester) async {
      _useDesktopView(tester);

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Center(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  const SizedBox(
                    width: 320,
                    child: TextField(
                      decoration: InputDecoration(
                        hintText: 'Pretraži',
                        isDense: true,
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  SizedBox(
                    width: 180,
                    child: DateField(
                      labelText: 'Od',
                      initialValue: DateTime(2026, 11, 1),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(
        tester.getSize(find.byType(TextField)).height,
        tester.getSize(find.byType(DateField)).height,
      );
    });

    testWidgets('search field and both date fields in EntityToolbar share one '
        'height', (tester) async {
      _useDesktopView(tester);

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: EntityToolbar(
              searchController: TextEditingController(),
              showSearch: true,
              searchHint: 'Pretraži po nazivu...',
              showAdd: false,
              filterBar: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SizedBox(width: 180, child: DateField(labelText: 'Od')),
                  const SizedBox(width: 12),
                  SizedBox(
                    width: 180,
                    child: DateField(
                      labelText: 'Do',
                      initialValue: DateTime(2026, 11, 1),
                    ),
                  ),
                  const SizedBox(width: 12),
                ],
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final searchHeight = tester
          .getSize(
            find.descendant(
              of: find.byType(EntityToolbar),
              matching: find.byType(TextField),
            ),
          )
          .height;

      expect(tester.getSize(find.byType(DateField).at(0)).height, searchHeight);
      expect(tester.getSize(find.byType(DateField).at(1)).height, searchHeight);
    });
  });
}
