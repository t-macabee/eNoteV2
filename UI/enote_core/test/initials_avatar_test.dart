import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  testWidgets('InitialsAvatar scales the font with the radius', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              InitialsAvatar(text: 'Ana Kovač', radius: 20),
              InitialsAvatar(text: 'Ana Kovač', radius: 40),
            ],
          ),
        ),
      ),
    );

    final labels = tester.widgetList<Text>(find.text('AK')).toList();

    expect(labels.first.style?.fontSize, 14);
    expect(labels.last.style?.fontSize, 28);
  });
}
