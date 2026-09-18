import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_provider.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

import 'helpers.dart';

void main() {
  group('CourseCatalogProvider', () {
    test('getCatalogInstructors fetches from instructor/courses/catalog/instructors', () async {
      final mockClient = ScriptedClient((req) async {
        expect(req.url.path, '/api/v1/instructor/courses/catalog/instructors');
        expect(req.method, 'GET');
        return http.Response(
          jsonEncode([
            {'id': 1, 'name': 'Professor Piano'},
            {'id': 2, 'name': 'Maestro Guitar'},
          ]),
          200,
        );
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
        httpClient: mockClient,
      );
      final provider = CourseCatalogProvider(apiClient: apiClient);

      final instructors = await provider.getCatalogInstructors();

      expect(instructors.length, 2);
      expect(instructors[0].id, 1);
      expect(instructors[0].name, 'Professor Piano');
      expect(instructors[1].id, 2);
      expect(instructors[1].name, 'Maestro Guitar');
    });

    test('getCatalogSummary fetches from instructor/courses/catalog/summary', () async {      final mockClient = ScriptedClient((req) async {
        expect(req.url.path, '/api/v1/instructor/courses/catalog/summary');
        expect(req.method, 'GET');
        return http.Response(
          jsonEncode({
            'totalCourses': 15,
            'totalStudents': 120,
          }),
          200,
        );
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
        httpClient: mockClient,
      );
      final provider = CourseCatalogProvider(apiClient: apiClient);

      final summary = await provider.getCatalogSummary();

      expect(summary.totalCourses, 15);
      expect(summary.totalStudents, 120);
    });

    test('F3-05: malformed 200 maps to ApiException, not FormatException',
        () async {
      final mockClient = ScriptedClient((req) async {
        return http.Response('not-json{{{', 200);
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
        httpClient: mockClient,
      );
      final provider = CourseCatalogProvider(apiClient: apiClient);

      expect(
        () => provider.getCatalogInstructors(),
        throwsA(isA<ApiException>()),
      );
    });
  });

  group('InstructorStudentProvider.getEnrollments', () {
    test('getEnrollments fetches from instructor/students/{id}/enrollments', () async {
      final mockClient = ScriptedClient((req) async {
        expect(req.url.path, '/api/v1/instructor/students/42/enrollments');
        expect(req.method, 'GET');
        return http.Response(
          jsonEncode([
            {
              'courseId': 101,
              'courseName': 'Cello Basics',
              'instructorId': 5,
              'instructorName': 'Ana Novak',
            },
          ]),
          200,
        );
      });

      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(),
        httpClient: mockClient,
      );
      final provider = InstructorStudentProvider(apiClient: apiClient);

      final enrollments = await provider.getEnrollments(42);

      expect(enrollments.length, 1);
      expect(enrollments[0].courseId, 101);
      expect(enrollments[0].courseName, 'Cello Basics');
      expect(enrollments[0].instructorId, 5);
      expect(enrollments[0].instructorName, 'Ana Novak');
    });
  });
}
