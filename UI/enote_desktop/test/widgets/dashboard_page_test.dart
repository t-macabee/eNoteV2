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

Widget _buildApp({List<Widget> cards = const []}) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(username: 'admin'),
  );

  return MultiProvider(
    providers: [ChangeNotifierProvider<AuthState>.value(value: authState)],
    child: MaterialApp(
      home: Scaffold(body: DashboardPage(cards: cards)),
    ),
  );
}

void main() {
  testWidgets('content is top-aligned and limited to the content max width', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(_buildApp());

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
    await tester.pumpWidget(_buildApp());

    expect(find.textContaining('admin'), findsOneWidget);
  });

  testWidgets('page shows no shortcut section', (WidgetTester tester) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(
        cards: const [
          DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: 4,
          ),
        ],
      ),
    );

    expect(find.text('Prečice'), findsNothing);
    expect(find.byType(OutlinedButton), findsNothing);
  });

  testWidgets('cards sit in one row when the content width allows it', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(
        cards: const [
          DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: 4,
          ),
          DashboardStatCard(
            icon: Icons.store_outlined,
            label: 'Trgovina muzičke opreme',
            value: 3,
          ),
          DashboardStatCard(
            icon: Icons.class_outlined,
            label: 'Kursevi',
            value: 7,
          ),
          DashboardStatCard(
            icon: Icons.event_outlined,
            label: 'Događaji',
            value: 5,
          ),
        ],
      ),
    );

    final cards = find.byType(DashboardStatCard);
    expect(cards, findsNWidgets(4));

    final top = tester.getTopLeft(cards.first).dy;
    for (var i = 1; i < 4; i++) {
      expect(tester.getTopLeft(cards.at(i)).dy, top);
    }
  });
}
