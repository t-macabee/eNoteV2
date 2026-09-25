import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_enrollments_dialog.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';

import 'helpers.dart';

Map<String, dynamic> _enrollmentJson({
  required int id,
  required String studentName,
  required String enrollmentStatus,
  String? paidUntil,
  String? decidedAt,
  String? decisionNote,
}) => {
  'id': id,
  'studentId': id,
  'studentName': studentName,
  'enrollmentStatus': enrollmentStatus,
  'paidUntil': paidUntil,
  'decidedAt': decidedAt,
  'decisionNote': decisionNote,
};

final _defaultRows = [
  _enrollmentJson(
    id: 11,
    studentName: 'Student Enote',
    enrollmentStatus: 'Pending',
  ),
  _enrollmentJson(
    id: 12,
    studentName: 'Aktivni Student',
    enrollmentStatus: 'Active',
    paidUntil: '2027-01-01T00:00:00',
    decidedAt: '2026-09-20T08:15:00',
  ),
];

ScriptedClient _client({List<Map<String, dynamic>>? items}) =>
    ScriptedClient((request) {
      final path = request.url.path;
      if (request.method == 'GET' && path.endsWith('/enrollments')) {
        final rows = items ?? _defaultRows;
        return jsonResponse({
          'items': rows,
          'page': 1,
          'pageSize': 20,
          'totalCount': rows.length,
        }, 200);
      }
      if (request.method == 'POST') {
        return jsonResponse(
          _enrollmentJson(
            id: 11,
            studentName: 'Student Enote',
            enrollmentStatus: path.endsWith('/approve') ? 'Active' : 'Rejected',
          ),
          200,
        );
      }
      return jsonResponse(const {}, 404);
    });

IconButton _button(WidgetTester tester, String tooltip) =>
    tester.widget<IconButton>(
      find.descendant(
        of: find.byTooltip(tooltip),
        matching: find.byType(IconButton),
      ),
    );

Future<void> pumpDialog(WidgetTester tester, ScriptedClient httpClient) async {
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
      child: const MaterialApp(
        home: Scaffold(
          body: CourseEnrollmentsDialog(courseId: 1, courseName: 'Gitara'),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('row actions are enabled only for the status they apply to', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient);

    expect(_button(tester, 'Odobri').onPressed, isNotNull);
    expect(
      _button(tester, 'Samo zahtjev na čekanju se može odobriti.').onPressed,
      isNull,
    );
    expect(_button(tester, 'Odbij').onPressed, isNotNull);
    expect(
      _button(tester, 'Samo zahtjev na čekanju se može odbiti.').onPressed,
      isNull,
    );
    expect(_button(tester, 'Položio').onPressed, isNotNull);
    expect(
      _button(tester, 'Samo aktivan upis može biti označen kao položen.')
          .onPressed,
      isNull,
    );
    expect(_button(tester, 'Odbij').color, Colors.red);
    expect(
      tester
          .widget<Icon>(
            find.descendant(
              of: find.byTooltip('Samo zahtjev na čekanju se može odbiti.'),
              matching: find.byType(Icon),
            ),
          )
          .color,
      isNull,
    );
  });

  testWidgets('Odobri confirms and POSTs approve, then reloads the list', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient);
    final listGetsBefore = httpClient.getUrls.length;

    await tester.tap(find.byTooltip('Odobri'));
    await tester.pumpAndSettle();
    expect(find.text('Odobri upis'), findsOneWidget);
    expect(
      find.text('Odobriti upis za studenta Student Enote?'),
      findsOneWidget,
    );

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.postUrls.length, 1);
    expect(
      httpClient.postUrls.single.endsWith(
        '/instructor/courses/1/enrollments/11/approve',
      ),
      isTrue,
    );
    expect(httpClient.getUrls.length, listGetsBefore + 1);
    expect(find.text('Upis odobren.'), findsOneWidget);
  });

  testWidgets('Odbij requires a reason and POSTs it in the body', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient);

    await tester.tap(find.byTooltip('Odbij'));
    await tester.pumpAndSettle();
    expect(find.text('Razlog *'), findsOneWidget);
    final confirmButton = tester.widget<ElevatedButton>(
      find.widgetWithText(ElevatedButton, 'Potvrdi'),
    );
    expect(confirmButton.onPressed, isNull);

    await tester.enterText(find.byType(TextField), 'Popunjeno');
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.postUrls.length, 1);
    expect(
      httpClient.postUrls.single.endsWith(
        '/instructor/courses/1/enrollments/11/reject',
      ),
      isTrue,
    );
    expect(
      jsonDecode(httpClient.postedBodies.single),
      equals({'reason': 'Popunjeno'}),
    );
    expect(find.text('Upis odbijen.'), findsOneWidget);
  });

  testWidgets('the first list request filters by Pending', (tester) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient);

    expect(httpClient.requests.first.method, 'GET');
    expect(
      httpClient.requests.first.url.queryParameters['enrollmentStatus'],
      'Pending',
    );
  });

  testWidgets('Odlučeno formats decidedAt and shows - when it is null', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpDialog(tester, httpClient);

    expect(find.text('Odlučeno: 20.09.2026.'), findsOneWidget);
    expect(find.text('Odlučeno: -'), findsOneWidget);
  });
}
