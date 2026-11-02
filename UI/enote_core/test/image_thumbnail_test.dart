import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

void main() {
  testWidgets('ImageThumbnail placeholder uses the surface container color', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: const Scaffold(body: ImageThumbnail(size: 40)),
      ),
    );

    final container = tester.widget<Container>(
      find
          .descendant(
            of: find.byType(ImagePlaceholder),
            matching: find.byType(Container),
          )
          .first,
    );
    final decoration = container.decoration! as BoxDecoration;
    expect(decoration.color, AppTheme.surfaceContainer);
  });
}
