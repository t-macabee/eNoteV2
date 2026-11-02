import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

void main() {
  testWidgets('InfoRow label uses bodySmall and value uses bodyMedium', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: const Scaffold(
          body: InfoRow(label: 'Email', value: 'ana@example.com'),
        ),
      ),
    );

    final context = tester.element(find.byType(InfoRow));
    final textTheme = Theme.of(context).textTheme;

    final label = tester.widget<Text>(find.text('Email'));
    expect(label.style?.fontSize, textTheme.bodySmall?.fontSize);

    final value = tester.widget<Text>(find.text('ana@example.com'));
    expect(value.style?.fontSize, textTheme.bodyMedium?.fontSize);
    expect(value.style?.fontWeight, FontWeight.w500);
  });
}
