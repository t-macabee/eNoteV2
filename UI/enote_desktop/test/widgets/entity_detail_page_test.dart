import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/widgets/entity_detail_page.dart';

void _setSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  testWidgets('shows title, close button and one tab label per tab', (
    WidgetTester tester,
  ) async {
    _setSize(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: EntityDetailPage(
          title: 'My Title',
          tabs: [
            EntityDetailTab(
              label: 'First',
              builder: (_) => const Text('First body'),
            ),
            EntityDetailTab(
              label: 'Second',
              builder: (_) => const Text('Second body'),
            ),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('My Title'), findsOneWidget);
    expect(find.byTooltip('Zatvori'), findsOneWidget);
    expect(find.text('First'), findsOneWidget);
    expect(find.text('Second'), findsOneWidget);
  });

  testWidgets('shows no TabBar for a single tab', (WidgetTester tester) async {
    _setSize(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: EntityDetailPage(
          title: 'Single',
          tabs: [
            EntityDetailTab(
              label: 'Only',
              builder: (_) => const Text('Only body'),
            ),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(TabBar), findsNothing);
  });

  testWidgets('switching tabs shows the other tab body', (
    WidgetTester tester,
  ) async {
    _setSize(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: EntityDetailPage(
          title: 'Switch',
          tabs: [
            EntityDetailTab(
              label: 'First',
              builder: (_) => const Text('First content'),
            ),
            EntityDetailTab(
              label: 'Second',
              builder: (_) => const Text('Second content'),
            ),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Second'));
    await tester.pumpAndSettle();

    expect(find.text('Second content'), findsOneWidget);
  });

  testWidgets('keeps tab state after switching away and back', (
    WidgetTester tester,
  ) async {
    _setSize(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: EntityDetailPage(
          title: 'Keep',
          tabs: [
            EntityDetailTab(label: 'First', builder: (_) => const TextField()),
            EntityDetailTab(
              label: 'Second',
              builder: (_) => const Text('Second content'),
            ),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'hello');
    await tester.pump();

    await tester.tap(find.text('Second'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('First'));
    await tester.pumpAndSettle();

    expect(find.text('hello'), findsOneWidget);
  });

  testWidgets('tabs align to the left of the content area', (
    WidgetTester tester,
  ) async {
    _setSize(tester);

    await tester.pumpWidget(
      MaterialApp(
        home: EntityDetailPage(
          title: 'Align',
          tabs: [
            EntityDetailTab(
              label: 'First',
              builder: (_) => const Text('First body'),
            ),
            EntityDetailTab(
              label: 'Second',
              builder: (_) => const Text('Second body'),
            ),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    final tabLeft = tester.getTopLeft(find.byType(Tab).first).dx;
    expect(tabLeft, lessThan(1280 * 0.25));
  });

  testWidgets('close button calls onClose when given, and pops otherwise', (
    WidgetTester tester,
  ) async {
    _setSize(tester);

    var closed = false;
    await tester.pumpWidget(
      MaterialApp(
        home: EntityDetailPage(
          title: 'CloseCb',
          onClose: () => closed = true,
          tabs: [
            EntityDetailTab(label: 'A', builder: (_) => const Text('a')),
            EntityDetailTab(label: 'B', builder: (_) => const Text('b')),
          ],
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pump();
    expect(closed, isTrue);

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => EntityDetailPage(
                  title: 'Popped',
                  tabs: [
                    EntityDetailTab(
                      label: 'A',
                      builder: (_) => const Text('a'),
                    ),
                  ],
                ),
              ),
            ),
            child: const Text('Open'),
          ),
        ),
      ),
    );
    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();
    expect(find.text('Popped'), findsOneWidget);

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();
    expect(find.text('Popped'), findsNothing);
  });
}
