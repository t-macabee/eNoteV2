import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_detail_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_enrollments_view.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/features/instructor/enrollment/instructor_enrollment_list_screen.dart';
import 'package:enote_desktop/features/instructor/enrollment/instructor_enrollment_provider.dart';

import '../../../helpers.dart';

Map<String, dynamic> _enrollmentJson({
  required int id,
  required String studentName,
  required int courseId,
  required String courseName,
  required String enrollmentStatus,
  String? paidUntil,
  String? decidedAt,
}) => {
  'id': id,
  'studentId': id,
  'studentName': studentName,
  'courseId': courseId,
  'courseName': courseName,
  'enrollmentStatus': enrollmentStatus,
  'paidUntil': paidUntil,
  'decidedAt': decidedAt,
};

List<Map<String, dynamic>> _rows() => [
  _enrollmentJson(
    id: 11,
    studentName: 'Student Enote',
    courseId: 7,
    courseName: 'Gitara',
    enrollmentStatus: 'Pending',
  ),
  _enrollmentJson(
    id: 12,
    studentName: 'Aktivni Student',
    courseId: 7,
    courseName: 'Gitara',
    enrollmentStatus: 'Active',
    paidUntil: '2027-01-01T00:00:00',
    decidedAt: '2026-09-20T08:15:00',
  ),
];

Map<String, dynamic> _courseJson() => {
  'id': 7,
  'instructorId': 10,
  'name': 'Gitara',
  'price': 100.0,
  'enrolledCount': 1,
  'isPublished': true,
  'startDate': '2026-09-01T00:00:00',
  'endDate': '2026-12-01T00:00:00',
};

ScriptedClient _client(List<Map<String, dynamic>> rows) =>
    ScriptedClient((request) {
      final path = request.url.path;
      final query = request.url.queryParameters;

      if (request.method == 'GET' && path.endsWith('/enrollments')) {
        final status = query['enrollmentStatus'];
        final courseId = query['courseId'];
        var matching = rows.where(
          (row) => status == null || row['enrollmentStatus'] == status,
        );
        if (courseId != null) {
          matching = matching.where(
            (row) => row['courseId'].toString() == courseId,
          );
        }
        final pageSize = int.parse(query['pageSize'] ?? '20');
        return jsonResponse({
          'items': matching.take(pageSize).toList(),
          'page': 1,
          'pageSize': pageSize,
          'totalCount': matching.length,
        }, 200);
      }

      if (request.method == 'GET' && path.contains('/instructor/courses/')) {
        return jsonResponse(_courseJson(), 200);
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

Future<void> pumpQueue(WidgetTester tester, ScriptedClient httpClient) async {
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
        ChangeNotifierProvider<InstructorEnrollmentProvider>.value(
          value: InstructorEnrollmentProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(
        home: Scaffold(body: InstructorEnrollmentListScreen()),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

List<RecordedRequest> _listRequests(ScriptedClient client) => [
  for (final request in client.recorded)
    if (request.method == 'GET' &&
        Uri.parse(request.url).path.endsWith('/enrollments') &&
        Uri.parse(request.url).queryParameters['pageSize'] != '1')
      request,
];

List<RecordedRequest> _countRequests(ScriptedClient client) => [
  for (final request in client.recorded)
    if (request.method == 'GET' &&
        Uri.parse(request.url).path.endsWith('/enrollments') &&
        Uri.parse(request.url).queryParameters['pageSize'] == '1')
      request,
];

Finder _actionButton(String label) => find.widgetWithText(TextButton, label);

void main() {
  testWidgets('queue opens on the pending tab with the pending filter', (
    tester,
  ) async {
    final httpClient = _client(_rows());
    await pumpQueue(tester, httpClient);

    final firstList = Uri.parse(_listRequests(httpClient).first.url);
    expect(firstList.path, endsWith('/instructor/enrollments'));
    expect(firstList.queryParameters['enrollmentStatus'], 'Pending');
    expect(find.text('Na čekanju (1)'), findsOneWidget);
  });

  testWidgets('pending tab shows the count', (tester) async {
    final httpClient = _client(_rows());
    await pumpQueue(tester, httpClient);

    expect(find.text('Na čekanju (1)'), findsOneWidget);
    expect(find.text('Svi'), findsOneWidget);
    final count = Uri.parse(_countRequests(httpClient).first.url);
    expect(count.path, endsWith('/instructor/enrollments'));
    expect(count.queryParameters['enrollmentStatus'], 'Pending');
  });

  testWidgets('pending row shows Odobri and Odbij', (tester) async {
    await pumpQueue(tester, _client(_rows()));

    expect(_actionButton('Odobri'), findsOneWidget);
    expect(_actionButton('Odbij'), findsOneWidget);
    expect(_actionButton('Položio'), findsNothing);
  });

  testWidgets('Odobri POSTs to the course route of the row', (tester) async {
    final httpClient = _client(_rows());
    await pumpQueue(tester, httpClient);

    await tester.tap(_actionButton('Odobri'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      httpClient.postUrls.single,
      endsWith('/instructor/courses/7/enrollments/11/approve'),
    );
    expect(find.text('Upis odobren.'), findsOneWidget);
  });

  testWidgets('count reloads after an action', (tester) async {
    final httpClient = _client(_rows());
    await pumpQueue(tester, httpClient);
    final countsBefore = _countRequests(httpClient).length;

    await tester.tap(_actionButton('Odobri'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(_countRequests(httpClient).length, countsBefore + 1);
    expect(find.text('Na čekanju (0)'), findsOneWidget);
  });

  testWidgets('changing the tab resets to page 1', (tester) async {
    final httpClient = _client(_rows());
    await pumpQueue(tester, httpClient);

    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    final lastList = Uri.parse(_listRequests(httpClient).last.url);
    expect(lastList.queryParameters['page'], '1');
    expect(lastList.queryParameters.containsKey('enrollmentStatus'), isFalse);
  });

  testWidgets('row tap opens the course detail on the Upisi tab', (
    tester,
  ) async {
    await pumpQueue(tester, _client(_rows()));

    await tester.tap(find.text('Student Enote'));
    await tester.pumpAndSettle();

    expect(find.byType(CourseDetailScreen), findsOneWidget);
    expect(find.byType(CourseEnrollmentsView), findsOneWidget);
    final controller = DefaultTabController.of(
      tester.element(find.byType(TabBar)),
    );
    expect(controller.index, CourseDetailTab.enrollments.index);
  });
}
