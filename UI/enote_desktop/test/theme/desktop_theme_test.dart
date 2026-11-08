import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/theme/app_theme.dart';

double _textWidth(FontWeight weight) {
  final painter = TextPainter(
    text: TextSpan(
      text: 'Odobri upis studenta',
      style: TextStyle(
        fontFamily: 'packages/enote_core/Roboto',
        fontSize: 16,
        fontWeight: weight,
      ),
    ),
    textDirection: TextDirection.ltr,
  )..layout();
  return painter.width;
}

void main() {
  test('desktop theme uses the Roboto family', () {
    expect(
      desktopTheme.textTheme.bodyMedium?.fontFamily,
      'packages/enote_core/Roboto',
    );
    expect(
      desktopTheme.textTheme.titleLarge?.fontFamily,
      'packages/enote_core/Roboto',
    );
    expect(
      desktopTheme.primaryTextTheme.bodyMedium?.fontFamily,
      'packages/enote_core/Roboto',
    );
  });

  test('desktop type scale matches the table', () {
    final textTheme = desktopTheme.textTheme;

    void expectRole(TextStyle? style, double fontSize, FontWeight weight) {
      expect(style?.fontSize, fontSize);
      expect(style?.fontWeight, weight);
      expect(style?.fontFamily, 'packages/enote_core/Roboto');
    }

    expectRole(textTheme.headlineMedium, 28, FontWeight.w600);
    expectRole(textTheme.titleLarge, 18, FontWeight.w600);
    expectRole(textTheme.titleMedium, 16, FontWeight.w600);
    expectRole(textTheme.titleSmall, 14, FontWeight.w600);
    expectRole(textTheme.bodyMedium, 14, FontWeight.w400);
    expectRole(textTheme.bodySmall, 13, FontWeight.w400);
    expectRole(textTheme.labelMedium, 12, FontWeight.w500);
    expectRole(textTheme.labelSmall, 11, FontWeight.w500);
  });

  test('w600 text resolves to the SemiBold face', () {
    final semiBold = _textWidth(FontWeight.w600);
    final bold = _textWidth(FontWeight.w700);

    expect(semiBold, lessThan(bold));
  });

  test('every dialog gets the dialog shell look from the theme', () {
    final dialogTheme = desktopTheme.dialogTheme;

    expect(dialogTheme.backgroundColor, AppTheme.surfaceContainer);
    expect(
      dialogTheme.shape,
      RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppRadius.lg),
        side: const BorderSide(color: AppTheme.outline),
      ),
    );
    expect(dialogTheme.titleTextStyle?.fontSize, 18);
    expect(dialogTheme.titleTextStyle?.fontWeight, FontWeight.w600);
    expect(dialogTheme.titleTextStyle?.color, AppTheme.textPrimary);
  });
}
