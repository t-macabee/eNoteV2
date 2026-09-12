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

  group('CourseDto.isEnrolled', () {
    test('missing key defaults to false', () {
      final dto = CourseDto.fromJson({'id': 1, 'name': 'n'});
      expect(dto.isEnrolled, isFalse);
    });

    test('true parses as true', () {
      final dto =
          CourseDto.fromJson({'id': 1, 'name': 'n', 'isEnrolled': true});
      expect(dto.isEnrolled, isTrue);
    });
  });

  group('LectureDto.myAttendanceStatus', () {
    test('missing key defaults to null', () {
      final dto = LectureDto.fromJson({'id': 1, 'name': 'n'});
      expect(dto.myAttendanceStatus, isNull);
    });

    test('null value defaults to null', () {
      final dto = LectureDto.fromJson(
          {'id': 1, 'name': 'n', 'myAttendanceStatus': null});
      expect(dto.myAttendanceStatus, isNull);
    });

    test('string value parses', () {
      final dto = LectureDto.fromJson(
          {'id': 1, 'name': 'n', 'myAttendanceStatus': 'present'});
      expect(dto.myAttendanceStatus, AttendanceStatus.present);
    });

    test('int value parses', () {
      final dto =
          LectureDto.fromJson({'id': 1, 'name': 'n', 'myAttendanceStatus': 2});
      expect(dto.myAttendanceStatus, AttendanceStatus.present);
    });
  });
}
