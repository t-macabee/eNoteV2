import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/shell/app_router.dart';

Widget _app() => MaterialApp(
      onGenerateRoute: AppRouter.onGenerateRoute,
      home: const Scaffold(body: Text('home')),
    );

Future<void> _pushNamed(WidgetTester tester, String name) async {
  final navigator = tester.state<NavigatorState>(find.byType(Navigator));
  unawaited(navigator.pushNamed(name, arguments: 'wrong'));
  await tester.pumpAndSettle();
}

void main() {
  const routesWithArguments = [
    AppRouter.courseDetail,
    AppRouter.lectureDetail,
    AppRouter.lectureNotes,
    AppRouter.lectureNoteDetail,
    AppRouter.assignmentDetail,
    AppRouter.ranking,
    AppRouter.announcementDetail,
    AppRouter.instrumentDetail,
    AppRouter.rentalDetail,
    AppRouter.payment,
    AppRouter.tuitionPayment,
    AppRouter.tuitionHistory,
    AppRouter.eventDetail,
  ];

  for (final name in routesWithArguments) {
    testWidgets('$name with a wrong argument type shows the unknown route', (
      tester,
    ) async {
      await tester.pumpWidget(_app());
      await _pushNamed(tester, name);

      expect(find.text('Pokušaj ponovo'), findsOneWidget);
    });
  }

  testWidgets('an unknown route shows the unknown route', (tester) async {
    await tester.pumpWidget(_app());
    await _pushNamed(tester, '/missing');

    expect(find.text('Pokušaj ponovo'), findsOneWidget);
  });
}
