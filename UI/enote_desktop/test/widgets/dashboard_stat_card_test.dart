import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/dashboard_stat_card.dart';

void main() {
  testWidgets('shows label and value', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: 42,
          ),
        ),
      ),
    );

    expect(find.text('Korisnici'), findsOneWidget);
    expect(find.text('42'), findsOneWidget);
  });

  testWidgets('shows a progress indicator while loading', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: null,
            isLoading: true,
          ),
        ),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.text('–'), findsNothing);
  });

  testWidgets('shows a dash when loading failed', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: null,
            hasError: true,
          ),
        ),
      ),
    );

    expect(find.text('–'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });

  testWidgets('calls onTap when tapped', (WidgetTester tester) async {
    var tapped = 0;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: 42,
            onTap: () => tapped++,
          ),
        ),
      ),
    );

    await tester.tap(find.text('Korisnici'));
    await tester.pump();

    expect(tapped, 1);
  });

  testWidgets('card has the stat card width token', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: 42,
          ),
        ),
      ),
    );

    expect(
      tester.getSize(find.byType(DashboardStatCard)).width,
      AppSize.statCardWidth,
    );
  });

  testWidgets('card without onTap has no InkWell', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DashboardStatCard(
            icon: Icons.people_outline,
            label: 'Korisnici',
            value: 42,
          ),
        ),
      ),
    );

    expect(find.byType(InkWell), findsNothing);
  });
}
