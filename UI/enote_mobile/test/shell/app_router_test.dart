import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/shell/routes.dart';

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
    AppRoutes.courseDetail,
    AppRoutes.lectureDetail,
    AppRoutes.lectureNotes,
    AppRoutes.lectureNoteDetail,
    AppRoutes.assignmentDetail,
    AppRoutes.ranking,
    AppRoutes.announcementDetail,
    AppRoutes.instrumentDetail,
    AppRoutes.rentalDetail,
    AppRoutes.payment,
    AppRoutes.tuitionPayment,
    AppRoutes.tuitionHistory,
    AppRoutes.eventDetail,
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
