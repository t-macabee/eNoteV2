import 'dart:convert';

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

  group('CourseDto tuition fields', () {
    test('missing keys default to null/false', () {
      final dto = CourseDto.fromJson({'id': 1, 'name': 'n'});
      expect(dto.enrollmentId, isNull);
      expect(dto.paidUntil, isNull);
      expect(dto.isFree, isFalse);
    });

    test('paid course parses enrollment id, paidUntil and isFree', () {
      final dto = CourseDto.fromJson({
        'id': 1,
        'name': 'n',
        'enrollmentId': 4,
        'paidUntil': '2026-10-09T12:41:00',
        'isFree': false,
      });
      expect(dto.enrollmentId, 4);
      expect(dto.paidUntil, DateTime.parse('2026-10-09T12:41:00'));
      expect(dto.isFree, isFalse);
    });

    test('free course parses isFree true', () {
      final dto =
          CourseDto.fromJson({'id': 1, 'name': 'n', 'isFree': true});
      expect(dto.isFree, isTrue);
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
          {'id': 1, 'name': 'n', 'myAttendanceStatus': 'Present'});
      expect(dto.myAttendanceStatus, AttendanceStatus.present);
    });
  });

  group('parseCourseRanking', () {
    test('parses a bare JSON list', () {
      final entries = parseCourseRanking(jsonEncode([
        {
          'rank': 1,
          'studentId': 7,
          'studentName': 'Student Enote',
          'averageGrade': 9.5,
          'gradedSubmissions': 3,
        },
        {
          'rank': 2,
          'studentId': 9,
          'studentName': 'Drugi Student',
          'averageGrade': null,
          'gradedSubmissions': 0,
        },
      ]));

      expect(entries, hasLength(2));
      expect(entries.first.rank, 1);
      expect(entries.first.studentName, 'Student Enote');
      expect(entries.first.averageGrade, 9.5);
      expect(entries.last.averageGrade, isNull);
    });

    test('empty body returns an empty list', () {
      expect(parseCourseRanking(''), isEmpty);
      expect(parseCourseRanking('  '), isEmpty);
    });
  });

  group('RsvpRequest', () {
    test('toJson carries only the confirm flag (no note field)', () {
      expect(RsvpRequest(confirm: true).toJson(), {'confirm': true});
      expect(RsvpRequest(confirm: false).toJson(), {'confirm': false});
    });
  });
}
