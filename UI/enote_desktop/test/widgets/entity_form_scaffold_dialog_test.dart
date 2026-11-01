import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';
import 'package:enote_desktop/widgets/entity_form_sections.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('dialog renders sections flat', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => EntityFormScaffold.showAsDialog(
                context,
                builder: (_) => EntityFormScaffold(
                  title: 'Test forma',
                  presentation: EntityFormPresentation.dialog,
                  fieldsBuilder: (_) => [
                    const FormSection(
                      title: 'Sekcija',
                      children: [Text('Polje')],
                    ),
                  ],
                  onSave: () async => true,
                ),
              ),
              child: const Text('Open'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(find.byType(FormSection), findsOneWidget);

    final content = find.descendant(
      of: find.byType(AlertDialog),
      matching: find.byType(SingleChildScrollView),
    );
    final containers = find.descendant(
      of: content,
      matching: find.byType(Container),
    );
    final hasBordered = tester.widgetList<Container>(containers).any((c) {
      final d = c.decoration;
      return d is BoxDecoration && d.border != null;
    });
    expect(hasBordered, isFalse);
  });

  testWidgets('dialog keeps save, cancel and close', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => EntityFormScaffold.showAsDialog(
                context,
                builder: (_) => EntityFormScaffold(
                  title: 'Test forma',
                  presentation: EntityFormPresentation.dialog,
                  fieldsBuilder: (_) => const [Text('Polje')],
                  onSave: () async => true,
                ),
              ),
              child: const Text('Open'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(find.text('Sačuvaj'), findsOneWidget);
    expect(find.text('Otkaži'), findsOneWidget);
    expect(find.byTooltip('Zatvori'), findsOneWidget);

    await tester.tap(find.text('Otkaži'));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
  });

  testWidgets(
    'dialog save with an invalid field shows the validator message and does not close',
    (tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => EntityFormScaffold.showAsDialog(
                  context,
                  builder: (_) => EntityFormScaffold(
                    title: 'Test forma',
                    presentation: EntityFormPresentation.dialog,
                    fieldsBuilder: (_) => [
                      TextFormField(
                        decoration: const InputDecoration(labelText: 'Naziv'),
                        validator: (value) => (value == null || value.isEmpty)
                            ? 'Obavezno'
                            : null,
                      ),
                    ],
                    onSave: () async => true,
                  ),
                ),
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
      await tester.pumpAndSettle();

      expect(find.text('Obavezno'), findsOneWidget);
      expect(find.byType(AlertDialog), findsOneWidget);
    },
  );

  testWidgets('dialog respects the width tokens', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => EntityFormScaffold.showAsDialog(
                context,
                builder: (_) => EntityFormScaffold(
                  title: 'Test forma',
                  presentation: EntityFormPresentation.dialog,
                  fieldsBuilder: (_) => const [Text('Polje')],
                  onSave: () async => true,
                ),
              ),
              child: const Text('Open'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(
      tester.getSize(find.byType(SingleChildScrollView)).width,
      AppSize.dialogContentWidth,
    );
  });
}
