import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

void main() {
  group('CourseCatalogInstructorDto', () {
    test('fromJson and toJson round-trip', () {
      final json = {'id': 5, 'name': 'John Doe'};
      final dto = CourseCatalogInstructorDto.fromJson(json);

      expect(dto.id, 5);
      expect(dto.name, 'John Doe');
      expect(dto.toJson(), equals(json));
    });

    test('fromJson handles null name and defaults id', () {
      final json = <String, dynamic>{};
      final dto = CourseCatalogInstructorDto.fromJson(json);

      expect(dto.id, 0);
      expect(dto.name, isNull);
    });
  });

  group('CourseCatalogSummaryDto', () {
    test('fromJson and toJson round-trip', () {
      final json = {'totalCourses': 42, 'totalStudents': 128};
      final dto = CourseCatalogSummaryDto.fromJson(json);

      expect(dto.totalCourses, 42);
      expect(dto.totalStudents, 128);
      expect(dto.toJson(), equals(json));
    });

    test('fromJson defaults to 0 when keys are missing', () {
      final json = <String, dynamic>{};
      final dto = CourseCatalogSummaryDto.fromJson(json);

      expect(dto.totalCourses, 0);
      expect(dto.totalStudents, 0);
    });
  });

  group('StudentEnrollmentDto', () {
    test('fromJson and toJson round-trip', () {
      final json = {
        'courseId': 101,
        'courseName': 'Guitar 101',
        'instructorId': 10,
        'instructorName': 'Jane Doe',
      };
      final dto = StudentEnrollmentDto.fromJson(json);

      expect(dto.courseId, 101);
      expect(dto.courseName, 'Guitar 101');
      expect(dto.instructorId, 10);
      expect(dto.instructorName, 'Jane Doe');
      expect(dto.toJson(), equals(json));
    });

    test('fromJson handles null instructorName', () {
      final json = {
        'courseId': 102,
        'courseName': 'Violin 101',
        'instructorId': 20,
      };
      final dto = StudentEnrollmentDto.fromJson(json);

      expect(dto.courseId, 102);
      expect(dto.courseName, 'Violin 101');
      expect(dto.instructorId, 20);
      expect(dto.instructorName, isNull);
    });
  });
}
