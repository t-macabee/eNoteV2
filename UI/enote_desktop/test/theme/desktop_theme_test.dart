import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/theme/app_theme.dart';

double _textWidth(FontWeight weight) {
  final painter = TextPainter(
    text: TextSpan(
      text: 'Odobri upis studenta',
      style: TextStyle(fontFamily: 'Roboto', fontSize: 16, fontWeight: weight),
    ),
    textDirection: TextDirection.ltr,
  )..layout();
  return painter.width;
}

void main() {
  test('desktop theme uses the Roboto family', () {
    expect(desktopTheme.textTheme.bodyMedium?.fontFamily, 'Roboto');
    expect(desktopTheme.textTheme.titleLarge?.fontFamily, 'Roboto');
    expect(desktopTheme.primaryTextTheme.bodyMedium?.fontFamily, 'Roboto');
  });

  test('w600 text resolves to the SemiBold face', () {
    final semiBold = _textWidth(FontWeight.w600);
    final bold = _textWidth(FontWeight.w700);

    expect(semiBold, lessThan(bold));
  });
}
