import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Future<List<bool?>> _pumpPage(WidgetTester tester, {int fieldCount = 3}) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final results = <bool?>[];
  await tester.pumpWidget(
    MaterialApp(
      home: Builder(
        builder: (context) => ElevatedButton(
          onPressed: () async {
            final result = await Navigator.of(context).push<bool>(
              MaterialPageRoute(
                builder: (_) => EntityFormScaffold(
                  title: 'Test forma',
                  fieldsBuilder: (_) => [
                    for (var i = 1; i <= fieldCount; i++)
                      TextFormField(
                        decoration: InputDecoration(labelText: 'Polje $i'),
                      ),
                  ],
                  onSave: () async => true,
                ),
              ),
            );
            results.add(result);
          },
          child: const Text('Otvori'),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Otvori'));
  await tester.pumpAndSettle();
  return results;
}

void main() {
  testWidgets('save button is placed in the bottom navigation bar', (
    tester,
  ) async {
    await _pumpPage(tester);

    final scaffold = tester.widget<Scaffold>(find.byType(Scaffold));
    expect(scaffold.bottomNavigationBar, isNotNull);
    expect(
      find.descendant(
        of: find.byWidget(scaffold.bottomNavigationBar!),
        matching: find.widgetWithText(FilledButton, 'Sačuvaj'),
      ),
      findsOneWidget,
    );
  });

  testWidgets('save button stays visible after the body scrolls', (
    tester,
  ) async {
    await _pumpPage(tester, fieldCount: 30);

    await tester.drag(find.byType(ListView), const Offset(0, -3000));
    await tester.pumpAndSettle();

    expect(find.text('Polje 30'), findsOneWidget);
    final saveButton = find.widgetWithText(FilledButton, 'Sačuvaj');
    expect(saveButton, findsOneWidget);
    final rect = tester.getRect(saveButton);
    expect(rect.top, greaterThanOrEqualTo(0));
    expect(rect.bottom, lessThanOrEqualTo(800));
  });

  testWidgets('Otkaži pops the route with the saved flag', (tester) async {
    final results = await _pumpPage(tester);

    await tester.tap(find.widgetWithText(TextButton, 'Otkaži'));
    await tester.pumpAndSettle();

    expect(find.byType(EntityFormScaffold), findsNothing);
    expect(results, [false]);
  });

  testWidgets('content width does not exceed AppSize.formMaxWidth', (
    tester,
  ) async {
    await _pumpPage(tester);

    final width = tester.getSize(find.byType(ListView)).width;
    expect(width, lessThanOrEqualTo(AppSize.formMaxWidth));
  });
}
