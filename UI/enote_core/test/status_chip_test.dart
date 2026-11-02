import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

void main() {
  testWidgets('StatusChip shows its label', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: StatusChip(label: 'Aktivno', tone: StatusTone.success),
        ),
      ),
    );

    expect(find.text('Aktivno'), findsOneWidget);
  });

  testWidgets('StatusChip text color equals the tone color', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: StatusChip(label: 'Odbijeno', tone: StatusTone.danger),
        ),
      ),
    );

    final label = tester.widget<Text>(find.text('Odbijeno'));
    expect(label.style?.color, StatusTone.danger.color);
  });

  testWidgets('StatusChip text uses the labelMedium size', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: const Scaffold(
          body: StatusChip(label: 'Aktivno', tone: StatusTone.success),
        ),
      ),
    );

    final label = tester.widget<Text>(find.text('Aktivno'));
    final context = tester.element(find.byType(StatusChip));
    expect(
      label.style?.fontSize,
      Theme.of(context).textTheme.labelMedium?.fontSize,
    );
  });
}
