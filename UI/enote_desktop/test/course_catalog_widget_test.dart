import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_provider.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_screen.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_details_dialog.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

class _MockHttpClient extends http.BaseClient {
  final Future<http.Response> Function(http.BaseRequest request) _handler;

  _MockHttpClient(this._handler);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final response = await _handler(request);
    return http.StreamedResponse(
      Stream.value(utf8.encode(response.body)),
      response.statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  testWidgets('CourseCatalogScreen renders catalog columns, no add button, and summary counts', (tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final mockClient = _MockHttpClient((req) async {
      if (req.url.path.endsWith('/instructors')) {
        return http.Response(jsonEncode([{'id': 1, 'name': 'Mozart'}]), 200);
      }
      if (req.url.path.endsWith('/summary')) {
        return http.Response(jsonEncode({'totalCourses': 42, 'totalStudents': 128}), 200);
      }
      return http.Response(
        jsonEncode({
          'items': [
            {
              'id': 1,
              'instructorId': 1,
              'name': 'Classical Piano',
              'price': 150.0,
              'isPublished': true,
              'enrolledCount': 10,
              'instructorName': 'Mozart',
              'startDate': '2026-09-01T00:00:00Z',
            }
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        }),
        200,
      );
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: mockClient,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider(
          create: (_) => CourseCatalogProvider(apiClient: apiClient),
          child: const CourseCatalogScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Katalog kurseva'), findsOneWidget);
    expect(find.text('Classical Piano'), findsOneWidget);
    expect(find.text('Mozart'), findsWidgets); // dropdown item and row column
    expect(find.text('150.00'), findsOneWidget);
    expect(find.text('10'), findsOneWidget);
    expect(find.text('42 kurseva · 128 studenata'), findsOneWidget);
    expect(find.byIcon(Icons.add), findsNothing);
  });

  testWidgets('InstructorStudentDetailsDialog renders student info and cross-enrollments', (tester) async {
    final mockClient = _MockHttpClient((req) async {
      if (req.url.path.endsWith('/enrollments')) {
        return http.Response(
          jsonEncode([
            {
              'courseId': 10,
              'courseName': 'Violin 101',
              'instructorId': 2,
              'instructorName': 'Beethoven',
            },
          ]),
          200,
        );
      }
      return http.Response(
        jsonEncode({
          'id': 5,
          'appUserId': 50,
          'firstName': 'Johann',
          'lastName': 'Bach',
          'username': 'jbach',
          'membershipPaidUntil': '2027-01-01T00:00:00Z',
        }),
        200,
      );
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: mockClient,
    );

    final student = StudentDto(
      id: 5,
      appUserId: 50,
      firstName: 'Johann',
      lastName: 'Bach',
      username: 'jbach',
      membershipPaidUntil: DateTime(2027, 1, 1),
    );

    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => InstructorStudentProvider(apiClient: apiClient),
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () {
                  showDialog(
                    context: context,
                    builder: (_) => InstructorStudentDetailsDialog(student: student),
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

    expect(find.text('Detalji studenta'), findsOneWidget);
    expect(find.text('Johann Bach'), findsOneWidget);
    expect(find.text('Status članarine'), findsOneWidget);
    expect(find.text('Upisani kursevi'), findsOneWidget);
    expect(find.text('Violin 101 — Beethoven'), findsOneWidget);
  });
}
