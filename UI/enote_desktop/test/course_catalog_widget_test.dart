import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_provider.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_screen.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_details_dialog.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

import 'helpers.dart';

void main() {
  testWidgets('CourseCatalogScreen renders catalog columns, no add button, and summary counts', (tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final mockClient = ScriptedClient((req) async {
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
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider(
            create: (_) => CourseCatalogProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: CourseCatalogScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Classical Piano'), findsOneWidget);
    expect(find.text('Mozart'), findsWidgets);
    expect(find.text('42 kurseva · 128 studenata'), findsOneWidget);
    expect(find.byIcon(Icons.add), findsNothing);
  });

  testWidgets('InstructorStudentDetailsDialog renders student info and cross-enrollments', (tester) async {
    final mockClient = ScriptedClient((req) async {
      if (req.url.path.endsWith('/membership-payments')) {
        return jsonResponse({
          'items': [
            {
              'id': 9,
              'studentId': 5,
              'amount': 40.0,
              'paidAt': '2026-09-01T00:00:00Z',
              'periodStart': '2026-09-01T00:00:00Z',
              'periodEnd': '2027-01-01T00:00:00Z',
              'receiptNumber': 'CL-9',
            },
          ],
          'page': 1,
          'pageSize': 5,
          'totalCount': 1,
        }, 200);
      }
      if (req.url.path.endsWith('/enrollments')) {
        return http.Response(
          jsonEncode({
            'items': [
              {
                'courseId': 10,
                'courseName': 'Violin 101',
                'instructorId': 2,
                'instructorName': 'Beethoven',
              },
            ],
            'page': 1,
            'pageSize': 5,
            'totalCount': 1,
          }),
          200,
        );
      }
      if (req.url.path.endsWith('/lectures')) {
        return jsonResponse({
          'items': [
            {
              'id': 31,
              'studentId': 5,
              'studentName': 'Johann Bach',
              'name': 'Individualni čas',
              'location': 'Učionica 2',
              'lectureType': 'Practical',
              'lectureStatus': 'Scheduled',
              'isCancelled': false,
              'lectureTime': '2026-09-14T18:00:00Z',
              'duration': 45,
              'capacity': 1,
              'attendeeCount': 0,
            },
          ],
          'page': 1,
          'pageSize': 100,
          'totalCount': 1,
        }, 200);
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
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider(
            create: (_) => InstructorStudentProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider(
            create: (_) => LectureProvider(apiClient: apiClient),
          ),
        ],
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
    expect(find.text('Članarina'), findsOneWidget);
    expect(find.textContaining('Potvrda CL-9'), findsOneWidget);
    expect(find.text('Upisani kursevi'), findsOneWidget);
    expect(find.text('Violin 101'), findsOneWidget);
    expect(find.text('Beethoven'), findsOneWidget);
    expect(find.text('Individualni plan'), findsOneWidget);
    expect(find.text('Individualni čas'), findsOneWidget);
  });
}
