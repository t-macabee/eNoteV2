import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/widgets/dashboard_count.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  testWidgets('starts in the loading state', (WidgetTester tester) async {
    _useDesktopView(tester);

    final count = DashboardCount();
    addTearDown(count.dispose);

    expect(count.isLoading, isTrue);
    expect(count.value, isNull);
    expect(count.hasError, isFalse);
  });

  testWidgets('load stores the value and stops loading', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);

    final count = DashboardCount();
    addTearDown(count.dispose);

    await count.load(() async => 42);

    expect(count.value, 42);
    expect(count.isLoading, isFalse);
    expect(count.hasError, isFalse);
  });

  testWidgets('load sets hasError and stops loading when fetch throws', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);

    final count = DashboardCount();
    addTearDown(count.dispose);

    await count.load(() async => throw Exception('failed'));

    expect(count.hasError, isTrue);
    expect(count.isLoading, isFalse);
  });

  testWidgets('load does not notify after dispose', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);

    final count = DashboardCount();
    var notifications = 0;
    count.addListener(() => notifications++);

    count.dispose();
    await count.load(() async => 42);

    expect(notifications, 0);
  });

  testWidgets('DashboardCountCard shows the value after load', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);

    final count = DashboardCount();
    addTearDown(count.dispose);
    await count.load(() async => 42);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: DashboardCountCard(
            count: count,
            icon: Icons.people_outline,
            label: 'Korisnici',
          ),
        ),
      ),
    );

    expect(find.text('Korisnici'), findsOneWidget);
    expect(find.text('42'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });

  testWidgets('DashboardCountCard shows a dash after a failed load', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);

    final count = DashboardCount();
    addTearDown(count.dispose);
    await count.load(() async => throw Exception('failed'));

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: DashboardCountCard(
            count: count,
            icon: Icons.people_outline,
            label: 'Korisnici',
          ),
        ),
      ),
    );

    expect(find.text('–'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });
}
