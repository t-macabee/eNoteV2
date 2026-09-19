import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_detail_dialog.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';

import 'helpers.dart';

ScriptedClient _client({String? failMessage}) => ScriptedClient((request) {
  if (failMessage != null &&
      (request.method == 'PUT' || request.method == 'DELETE')) {
    return jsonResponse({'message': failMessage}, 400);
  }
  if (request.method == 'PUT' &&
      request.url.path.endsWith('instructor/courses/1')) {
    final body = jsonDecode(request.body) as Map<String, dynamic>;
    return jsonResponse({
      'id': 1,
      'instructorId': 10,
      'name': body['name'] ?? 'Gitara',
      'price': 100.0,
      'enrolledCount': 3,
      'isPublished': body['isPublished'] ?? true,
      'startDate': body['startDate'],
      'endDate': body['endDate'],
    }, 200);
  }

  return jsonResponse(const {
    'items': [],
    'page': 1,
    'pageSize': 20,
    'totalCount': 0,
  }, 200);
});

void main() {
  CourseDto course({bool published = true}) => CourseDto(
        id: 1,
        instructorId: 10,
        name: 'Gitara',
        isPublished: published,
        price: 100,
        enrolledCount: 3,
        startDate: DateTime(2026, 9, 1),
        endDate: DateTime(2026, 12, 1),
      );

  Future<void> pumpDialog(
    WidgetTester tester,
    ScriptedClient httpClient,
    CourseDto dto,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: httpClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<CourseProvider>.value(
            value: CourseProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(body: CourseDetailDialog(course: dto)),
        ),
      ),
    );
    await tester.pump();
  }

  testWidgets(
      'unpublishing shows confirm dialog and does not PUT until confirmed',
      (tester) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient, course(published: true));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsOneWidget);
    expect(find.text('Potvrdite povlačenje kursa'), findsOneWidget);
    expect(find.text('Potvrdi'), findsOneWidget);
    expect(httpClient.putUrls, isEmpty,
        reason: 'no request may leave before the dialog is confirmed');

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.putUrls.length, 1);
    final body =
        jsonDecode(httpClient.putBodies.single)
            as Map<String, dynamic>;
    expect(body['isPublished'], isFalse);
  });

  testWidgets('cancelling the unpublish confirm dialog does not PUT',
      (tester) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient, course(published: true));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(TextButton, 'Otkaži'));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(httpClient.putUrls, isEmpty);
  });

  testWidgets('publishing an unpublished course PUTs without confirmation',
      (tester) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient, course(published: false));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(httpClient.putUrls.length, 1);
    final body =
        jsonDecode(httpClient.putBodies.single)
            as Map<String, dynamic>;
    expect(body['isPublished'], isTrue);
  });

  testWidgets('a failed publish shows the banner and re-enables the switch',
      (tester) async {
    final httpClient = _client(failMessage: 'Izmjena kursa nije uspjela.');
    await pumpDialog(tester, httpClient, course(published: false));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(httpClient.putUrls.length, 1);
    expect(find.text('Izmjena kursa nije uspjela.'), findsOneWidget);
    final switchWidget = tester.widget<Switch>(find.byType(Switch));
    expect(switchWidget.onChanged, isNotNull);
    expect(tester.takeException(), isNull);
  });

  testWidgets('a failed delete shows the banner and keeps the dialog open',
      (tester) async {
    final httpClient = _client(failMessage: 'Brisanje kursa nije uspjelo.');
    await pumpDialog(tester, httpClient, course());

    await tester.tap(find.widgetWithText(OutlinedButton, 'Obriši'));
    await tester.pumpAndSettle();
    expect(find.text('Potvrdite brisanje'), findsOneWidget);

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.deletedUrls.length, 1);
    expect(find.text('Brisanje kursa nije uspjelo.'), findsOneWidget);
    expect(find.byType(CourseDetailDialog), findsOneWidget);
    final deleteButton = tester.widget<OutlinedButton>(
      find.widgetWithText(OutlinedButton, 'Obriši'),
    );
    expect(deleteButton.onPressed, isNotNull);
    expect(tester.takeException(), isNull);
  });
}
