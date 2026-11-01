import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/dashboard_page.dart';
import 'package:enote_desktop/widgets/dashboard_stat_card.dart';

import '../helpers.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

Widget _buildApp({required List<DashboardShortcut> shortcuts}) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(username: 'admin'),
  );

  return MultiProvider(
    providers: [ChangeNotifierProvider<AuthState>.value(value: authState)],
    child: MaterialApp(
      home: Scaffold(
        body: DashboardPage(
          cards: const [
            DashboardStatCard(
              icon: Icons.people_outline,
              label: 'Korisnici',
              value: 4,
            ),
          ],
          shortcuts: shortcuts,
        ),
      ),
    ),
  );
}

void main() {
  testWidgets('content is top-aligned and limited to the content max width', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp(shortcuts: const []));

    final content = find.byWidgetPredicate(
      (widget) =>
          widget is ConstrainedBox &&
          widget.constraints.maxWidth == AppSize.contentMaxWidth,
    );

    expect(content, findsOneWidget);

    final align = tester.widget<Align>(
      find.ancestor(of: content, matching: find.byType(Align)),
    );
    expect(align.alignment, Alignment.topCenter);
  });

  testWidgets('shows the welcome line with the username', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp(shortcuts: const []));

    expect(find.textContaining('admin'), findsOneWidget);
  });

  testWidgets('shortcut button calls the navigation callback with its label', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    String? opened;
    await tester.pumpWidget(
      _buildApp(
        shortcuts: [
          DashboardShortcut(
            label: 'Kursevi',
            icon: Icons.class_outlined,
            onTap: () => opened = 'Kursevi',
          ),
        ],
      ),
    );

    await tester.tap(find.widgetWithText(OutlinedButton, 'Kursevi'));
    await tester.pump();

    expect(opened, 'Kursevi');
  });
}
