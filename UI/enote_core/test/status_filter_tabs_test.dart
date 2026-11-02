import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

void main() {
  const options = [
    StatusFilterOption<String>(label: 'Na čekanju', value: 'pending', count: 3),
    StatusFilterOption<String>(label: 'Odobreno', value: 'approved'),
    StatusFilterOption<String>(label: 'Svi', value: 'all'),
  ];

  Future<void> pumpTabs(
    WidgetTester tester, {
    String selected = 'pending',
    ValueChanged<String>? onChanged,
  }) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: Scaffold(
          body: Align(
            alignment: Alignment.topLeft,
            child: StatusFilterTabs<String>(
              options: options,
              selected: selected,
              onChanged: onChanged ?? (_) {},
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('shows one chip per option', (tester) async {
    await pumpTabs(tester);

    expect(find.byType(ChoiceChip), findsNWidgets(options.length));
    expect(find.text('Odobreno'), findsOneWidget);
    expect(find.text('Svi'), findsOneWidget);
  });

  testWidgets('shows the count in the label when given', (tester) async {
    await pumpTabs(tester);

    expect(find.text('Na čekanju (3)'), findsOneWidget);
    expect(find.text('Odobreno'), findsOneWidget);
  });

  testWidgets('tapping a chip calls onChanged with its value', (tester) async {
    String? changed;
    await pumpTabs(tester, onChanged: (value) => changed = value);

    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    expect(changed, 'all');
  });

  testWidgets('selected chip is marked selected', (tester) async {
    await pumpTabs(tester, selected: 'approved');

    final selected = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Odobreno'),
    );
    final unselected = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Na čekanju (3)'),
    );

    expect(selected.selected, isTrue);
    expect(unselected.selected, isFalse);
  });

  testWidgets('scrollable status tabs render in one horizontal row', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    const eightOptions = [
      StatusFilterOption<String>(label: 'Prvi filter', value: 'a'),
      StatusFilterOption<String>(label: 'Drugi filter', value: 'b'),
      StatusFilterOption<String>(label: 'Treći filter', value: 'c'),
      StatusFilterOption<String>(label: 'Četvrti filter', value: 'd'),
      StatusFilterOption<String>(label: 'Peti filter', value: 'e'),
      StatusFilterOption<String>(label: 'Šesti filter', value: 'f'),
      StatusFilterOption<String>(label: 'Sedmi filter', value: 'g'),
      StatusFilterOption<String>(label: 'Svi', value: 'all'),
    ];

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: Scaffold(
          body: StatusFilterTabs<String>(
            options: eightOptions,
            selected: 'all',
            onChanged: (_) {},
            scrollable: true,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final tops = eightOptions
        .map(
          (option) => tester
              .getTopLeft(find.widgetWithText(ChoiceChip, option.label))
              .dy,
        )
        .toSet();
    expect(tops, hasLength(1));

    final scrollable = tester.state<ScrollableState>(find.byType(Scrollable));
    expect(scrollable.position.maxScrollExtent, greaterThan(0));
  });
}
