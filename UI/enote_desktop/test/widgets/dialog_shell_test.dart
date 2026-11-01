import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/dialog_shell.dart';

void main() {
  void setSurface(WidgetTester tester) {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
  }

  Future<void> pumpShell(WidgetTester tester, Widget shell) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: Center(child: shell)),
      ),
    );
  }

  Finder paddingAroundTitle(String title) => find.ancestor(
    of: find.text(title),
    matching: find.byWidgetPredicate(
      (widget) => widget is Padding && widget.child is Row,
    ),
  );

  testWidgets('title and close button render in the default header', (
    tester,
  ) async {
    setSurface(tester);

    await pumpShell(
      tester,
      const DialogShell(title: 'Naslov', body: Text('Tijelo')),
    );

    expect(find.text('Naslov'), findsOneWidget);
    expect(find.byTooltip('Zatvori'), findsOneWidget);
  });

  testWidgets('close button pops the dialog when onClose is null', (
    tester,
  ) async {
    setSurface(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => TextButton(
              onPressed: () => showDialog<void>(
                context: context,
                builder: (_) =>
                    const DialogShell(title: 'Naslov', body: Text('Tijelo')),
              ),
              child: const Text('otvori'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('otvori'));
    await tester.pumpAndSettle();
    expect(find.text('Naslov'), findsOneWidget);

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(find.byType(Dialog), findsNothing);
    expect(find.text('Naslov'), findsNothing);
  });

  testWidgets('close button calls onClose when given', (tester) async {
    setSurface(tester);

    var closed = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => TextButton(
              onPressed: () => showDialog<void>(
                context: context,
                builder: (_) => DialogShell(
                  title: 'Naslov',
                  body: const Text('Tijelo'),
                  onClose: () => closed++,
                ),
              ),
              child: const Text('otvori'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('otvori'));
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(closed, 1);
    expect(find.text('Naslov'), findsOneWidget);
  });

  testWidgets('card radius uses the large radius token', (tester) async {
    setSurface(tester);

    await pumpShell(
      tester,
      const DialogShell(title: 'Naslov', body: Text('Tijelo')),
    );

    final dialog = tester.widget<Dialog>(find.byType(Dialog));
    final shape = dialog.shape! as RoundedRectangleBorder;

    expect(shape.borderRadius, BorderRadius.circular(AppRadius.lg));
  });

  testWidgets('default header uses the spacing tokens', (tester) async {
    setSurface(tester);

    await pumpShell(
      tester,
      const DialogShell(title: 'Naslov', body: Text('Tijelo')),
    );

    final padding = tester.widget<Padding>(paddingAroundTitle('Naslov'));

    expect(
      padding.padding,
      const EdgeInsets.fromLTRB(
        AppSpacing.xl,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
      ),
    );
  });

  testWidgets('custom header replaces the default header', (tester) async {
    setSurface(tester);

    await pumpShell(
      tester,
      DialogShell(
        title: 'Naslov',
        header: DialogShellHeader(
          label: const Text('Prilagođeno'),
          onClose: () {},
        ),
        body: const Text('Tijelo'),
      ),
    );

    expect(find.text('Prilagođeno'), findsOneWidget);
    expect(find.text('Naslov'), findsNothing);
  });

  testWidgets('header padding uses the spacing tokens', (tester) async {
    setSurface(tester);

    await pumpShell(
      tester,
      DialogShellHeader(label: const Text('Naslov'), onClose: () {}),
    );

    final padding = tester.widget<Padding>(paddingAroundTitle('Naslov'));

    expect(
      padding.padding,
      const EdgeInsets.fromLTRB(
        AppSpacing.sm,
        AppSpacing.xs,
        AppSpacing.sm,
        AppSpacing.xs,
      ),
    );
  });

  testWidgets('width enum caps the dialog width', (tester) async {
    setSurface(tester);

    await pumpShell(
      tester,
      const DialogShell(
        title: 'Naslov',
        width: DialogShellWidth.sm,
        body: Text('Tijelo'),
      ),
    );

    final constrained = tester.widget<ConstrainedBox>(
      find.byWidgetPredicate(
        (widget) =>
            widget is ConstrainedBox &&
            widget.constraints.maxWidth == DialogShellWidth.sm.value,
      ),
    );

    expect(constrained.constraints.maxWidth, 480);
  });
}
