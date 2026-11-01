import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/theme/app_theme.dart';
import 'package:enote_desktop/widgets/status_filter_tabs.dart';

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
}
