import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_enrollments_view.dart';
import 'package:enote_desktop/features/instructor/enrollment/instructor_enrollment_provider.dart';

import '../../../helpers.dart';

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
  'courseId': 1,
  'courseName': 'Gitara',
  'enrollmentStatus': enrollmentStatus,
  'paidUntil': paidUntil,
  'decidedAt': decidedAt,
  'decisionNote': decisionNote,
};

List<Map<String, dynamic>> _defaultRows() => [
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

ScriptedClient _client(List<Map<String, dynamic>> rows) =>
    ScriptedClient((request) {
      final path = request.url.path;
      if (request.method == 'GET' && path.endsWith('/enrollments')) {
        final filter = request.url.queryParameters['enrollmentStatus'];
        final matching = filter == null
            ? rows
            : rows.where((row) => row['enrollmentStatus'] == filter).toList();
        final pageSize = request.url.queryParameters['pageSize'];
        final items = pageSize == '1' ? matching.take(1).toList() : matching;
        return jsonResponse({
          'items': items,
          'page': 1,
          'pageSize': int.parse(pageSize ?? '20'),
          'totalCount': matching.length,
        }, 200);
      }
      if (request.method == 'POST') {
        final segments = request.url.pathSegments;
        final id = int.parse(segments[segments.indexOf('enrollments') + 1]);
        final newStatus = switch (segments.last) {
          'approve' => 'Active',
          'reject' => 'Rejected',
          _ => 'Completed',
        };
        final row = rows.firstWhere((row) => row['id'] == id);
        row['enrollmentStatus'] = newStatus;
        return jsonResponse(row, 200);
      }
      return jsonResponse(const {}, 404);
    });

Future<void> pumpView(
  WidgetTester tester,
  ScriptedClient httpClient, {
  VoidCallback? onMutation,
  int? pendingCount = 1,
}) async {
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
        ChangeNotifierProvider<InstructorEnrollmentProvider>.value(
          value: InstructorEnrollmentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: CourseEnrollmentsView(
            courseId: 1,
            pendingCount: pendingCount,
            onMutation: onMutation ?? () {},
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

List<RecordedRequest> _listRequests(ScriptedClient client) => [
  for (final request in client.recorded)
    if (request.method == 'GET' &&
        Uri.parse(request.url).queryParameters['pageSize'] != '1')
      request,
];

Finder _actionButton(String label) => find.widgetWithText(TextButton, label);

void main() {
  testWidgets('opens on the pending tab with the pending filter', (
    tester,
  ) async {
    final httpClient = _client(_defaultRows());
    await pumpView(tester, httpClient);

    final firstList = Uri.parse(_listRequests(httpClient).first.url);
    expect(firstList.queryParameters['enrollmentStatus'], 'Pending');
    expect(find.text('Na čekanju (1)'), findsOneWidget);
  });

  testWidgets('pending tab shows the count', (tester) async {
    await pumpView(tester, _client(_defaultRows()), pendingCount: 1);

    expect(find.text('Na čekanju (1)'), findsOneWidget);
    expect(find.text('Svi'), findsOneWidget);
  });

  testWidgets('pending row shows Odobri and Odbij', (tester) async {
    await pumpView(tester, _client(_defaultRows()));

    expect(_actionButton('Odobri'), findsOneWidget);
    expect(_actionButton('Odbij'), findsOneWidget);
    expect(_actionButton('Položio'), findsNothing);
  });

  testWidgets('active row shows only Položio', (tester) async {
    await pumpView(tester, _client([_defaultRows()[1]]));
    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    expect(_actionButton('Položio'), findsOneWidget);
    expect(_actionButton('Odobri'), findsNothing);
    expect(_actionButton('Odbij'), findsNothing);
  });

  testWidgets('rejected row shows no actions', (tester) async {
    final rejected = _enrollmentJson(
      id: 13,
      studentName: 'Odbijeni Student',
      enrollmentStatus: 'Rejected',
    );
    await pumpView(tester, _client([rejected]));
    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    expect(find.text('Odbijeni Student'), findsOneWidget);
    expect(_actionButton('Odobri'), findsNothing);
    expect(_actionButton('Odbij'), findsNothing);
    expect(_actionButton('Položio'), findsNothing);
  });

  testWidgets(
    'Odobri dialog shows the title Odobri upis and the student name',
    (tester) async {
      await pumpView(tester, _client(_defaultRows()));

      await tester.tap(_actionButton('Odobri'));
      await tester.pumpAndSettle();

      expect(find.text('Odobri upis'), findsOneWidget);
      expect(
        find.text('Odobriti upis za studenta Student Enote?'),
        findsOneWidget,
      );
    },
  );

  testWidgets('Odobri confirms and POSTs approve, then reloads the list', (
    tester,
  ) async {
    final httpClient = _client(_defaultRows());
    await pumpView(tester, httpClient);
    final listGetsBefore = _listRequests(httpClient).length;

    await tester.tap(_actionButton('Odobri'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.postUrls.length, 1);
    expect(
      httpClient.postUrls.single.endsWith(
        '/instructor/courses/1/enrollments/11/approve',
      ),
      isTrue,
    );
    expect(_listRequests(httpClient).length, listGetsBefore + 1);
    expect(find.text('Upis odobren.'), findsOneWidget);
  });

  testWidgets('count reloads after approve', (tester) async {
    var mutations = 0;
    final httpClient = _client(_defaultRows());
    await pumpView(
      tester,
      httpClient,
      pendingCount: 1,
      onMutation: () => mutations++,
    );

    await tester.tap(_actionButton('Odobri'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(mutations, 1);
  });

  testWidgets('a successful approve calls onMutation', (tester) async {
    var mutations = 0;
    await pumpView(
      tester,
      _client(_defaultRows()),
      onMutation: () => mutations++,
    );

    await tester.tap(_actionButton('Odobri'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(mutations, 1);
  });

  testWidgets('Odbij requires a reason and POSTs it in the body', (
    tester,
  ) async {
    final httpClient = _client(_defaultRows());
    await pumpView(tester, httpClient);

    await tester.tap(_actionButton('Odbij'));
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

  testWidgets('changing the tab resets to page 1', (tester) async {
    final httpClient = _client(_defaultRows());
    await pumpView(tester, httpClient);

    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    final lastList = Uri.parse(_listRequests(httpClient).last.url);
    expect(lastList.queryParameters['page'], '1');
    expect(lastList.queryParameters.containsKey('enrollmentStatus'), isFalse);
  });

  testWidgets('Odlučeno formats decidedAt and shows - when it is null', (
    tester,
  ) async {
    final httpClient = _client(_defaultRows());
    await pumpView(tester, httpClient);
    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    expect(find.text('Odlučeno: 20.09.2026.'), findsOneWidget);
    expect(find.text('Odlučeno: -'), findsOneWidget);
  });
}
