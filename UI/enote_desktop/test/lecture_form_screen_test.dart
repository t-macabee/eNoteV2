import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_form_screen.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';
import 'package:enote_desktop/widgets/entity_text_field.dart';

import 'helpers.dart';

void main() {
  testWidgets('F3-08: cancelled lecture disables Save', (tester) async {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: ScriptedClient((_) => jsonResponse(const {}, 200)),
    );
    final lectureProvider = LectureProvider(apiClient: apiClient);

    final cancelled = LectureDto(
      id: 3,
      name: 'Otkazano',
      location: 'Sala 1',
      lectureType: LectureType.theoretical,
      lectureStatus: LectureStatus.cancelled,
      isCancelled: true,
      lectureTime: DateTime(2026, 10, 1, 10),
      duration: 90,
      attendeeCount: 0,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<LectureProvider>.value(
        value: lectureProvider,
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () {
                  EntityFormScaffold.showAsDialog(
                    context,
                    builder: (_) => LectureFormScreen(
                      courseId: 1,
                      existing: cancelled,
                      presentation: EntityFormPresentation.dialog,
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(find.text('Ovo predavanje je otkazano i ne može se uređivati.'),
        findsOneWidget);

    final save = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Sačuvaj'),
    );
    expect(save.onPressed, isNull);
  });

  testWidgets('individual lecture loads course students and fixes capacity',
      (tester) async {
    final client = ScriptedClient((request) {
      if (request.url.path.endsWith('/enrollments')) {
        return jsonResponse({
          'items': [
            {
              'id': 4,
              'studentId': 7,
              'studentName': 'Amar Hodžić',
              'enrollmentStatus': 'Active',
            },
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 1,
        }, 200);
      }
      return jsonResponse(const {}, 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: client,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<LectureProvider>(
            create: (_) => LectureProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<CourseProvider>(
            create: (_) => CourseProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () {
                  EntityFormScaffold.showAsDialog(
                    context,
                    builder: (_) => const LectureFormScreen(
                      courseId: 3,
                      presentation: EntityFormPresentation.dialog,
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(find.text('Student'), findsNothing);

    await tester.tap(find.text('Individualno'));
    await tester.pumpAndSettle();

    expect(
      client.getUrls.singleWhere((url) => url.contains('/enrollments')),
      contains('enrollmentStatus=Active'),
    );
    expect(find.text('Student'), findsOneWidget);

    expect(
      find.widgetWithText(DropdownButtonFormField<int>, 'Student'),
      findsOneWidget,
    );

    final capacity = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(EntityTextField, 'Kapacitet (opcionalno)'),
        matching: find.byType(TextField),
      ),
    );
    expect(capacity.controller?.text, '1');
    expect(capacity.enabled, isFalse);
  });

  testWidgets('individual lecture edit shows the student name read-only',
      (tester) async {
    final client = ScriptedClient((request) {
      if (request.url.path.endsWith('/enrollments')) {
        return jsonResponse({
          'items': [
            {
              'id': 5,
              'studentId': 9,
              'studentName': 'Drugi Student',
              'enrollmentStatus': 'Active',
            },
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 1,
        }, 200);
      }
      return jsonResponse(const {}, 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: client,
    );

    final individual = LectureDto(
      id: 8,
      studentId: 7,
      studentName: 'Amar Hodžić',
      name: 'Individualni čas',
      location: 'Sala 1',
      lectureType: LectureType.theoretical,
      lectureStatus: LectureStatus.scheduled,
      isCancelled: false,
      lectureTime: DateTime(2026, 11, 5, 10),
      duration: 60,
      capacity: 1,
      attendeeCount: 0,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<LectureProvider>(
            create: (_) => LectureProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<CourseProvider>(
            create: (_) => CourseProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () {
                  EntityFormScaffold.showAsDialog(
                    context,
                    builder: (_) => LectureFormScreen(
                      courseId: 3,
                      existing: individual,
                      presentation: EntityFormPresentation.dialog,
                    ),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    expect(client.getUrls.where((url) => url.contains('/enrollments')), isEmpty);
    expect(find.widgetWithText(DropdownButtonFormField<int>, 'Student'),
        findsNothing);
    expect(find.text('Amar Hodžić'), findsOneWidget);

    final studentName = tester.widget<TextField>(
      find.descendant(
        of: find.widgetWithText(EntityTextField, 'Student'),
        matching: find.byType(TextField),
      ),
    );
    expect(studentName.controller?.text, 'Amar Hodžić');
    expect(studentName.enabled, isFalse);
  });
}
