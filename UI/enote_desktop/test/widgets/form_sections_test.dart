import 'package:enote_desktop/widgets/entity_form_sections.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Future<void> _pumpInWidth(
  WidgetTester tester,
  double width,
  Widget child,
) async {
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: Center(
          child: SizedBox(width: width, child: child),
        ),
      ),
    ),
  );
}

void main() {
  testWidgets('FormFieldRow lays fields in a Row at 800 px', (tester) async {
    await _pumpInWidth(
      tester,
      800,
      const FormFieldRow(children: [Text('Model'), Text('Proizvođač')]),
    );

    expect(
      find.descendant(
        of: find.byType(FormFieldRow),
        matching: find.byType(Row),
      ),
      findsOneWidget,
    );
    expect(
      tester.getTopLeft(find.text('Model')).dy,
      tester.getTopLeft(find.text('Proizvođač')).dy,
    );
  });

  testWidgets('FormFieldRow lays fields in a Column at 400 px', (tester) async {
    await _pumpInWidth(
      tester,
      400,
      const FormFieldRow(children: [Text('Model'), Text('Proizvođač')]),
    );

    expect(
      find.descendant(
        of: find.byType(FormFieldRow),
        matching: find.byType(Column),
      ),
      findsOneWidget,
    );
    expect(
      tester.getTopLeft(find.text('Proizvođač')).dy,
      greaterThan(tester.getTopLeft(find.text('Model')).dy),
    );
  });

  testWidgets('FormSection shows its title, description and children', (
    tester,
  ) async {
    await _pumpInWidth(
      tester,
      600,
      const FormSection(
        title: 'Osnovni podaci',
        description: 'Opis sekcije',
        children: [Text('Polje A'), Text('Polje B')],
      ),
    );

    expect(find.text('Osnovni podaci'), findsOneWidget);
    expect(find.text('Opis sekcije'), findsOneWidget);
    expect(find.text('Polje A'), findsOneWidget);
    expect(find.text('Polje B'), findsOneWidget);
  });
}
