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

  group('CourseDto enrollment fields', () {
    test('missing keys default to null', () {
      final dto = CourseDto.fromJson({'id': 1, 'name': 'n'});
      expect(dto.enrollmentStatus, isNull);
      expect(dto.enrollmentDecisionNote, isNull);
    });

    test('rejected request parses the status and the decision note', () {
      final dto = CourseDto.fromJson({
        'id': 1,
        'name': 'n',
        'enrollmentStatus': 'Rejected',
        'enrollmentDecisionNote': 'Grupa za ovaj termin je popunjena.',
      });
      expect(dto.enrollmentStatus, EnrollmentStatus.rejected);
      expect(dto.enrollmentDecisionNote, 'Grupa za ovaj termin je popunjena.');
    });
  });

  group('CourseEnrollmentDto', () {
    test('fromJson parses every field', () {
      final dto = CourseEnrollmentDto.fromJson({
        'id': 3,
        'studentId': 7,
        'studentName': 'Student Enote',
        'enrollmentStatus': 'Pending',
        'paidUntil': '2026-10-09T12:41:00',
        'decidedAt': '2026-09-20T08:15:00',
        'decisionNote': 'Grupa za ovaj termin je popunjena.',
      });
      expect(dto.id, 3);
      expect(dto.studentId, 7);
      expect(dto.studentName, 'Student Enote');
      expect(dto.enrollmentStatus, EnrollmentStatus.pending);
      expect(dto.paidUntil, DateTime.parse('2026-10-09T12:41:00'));
      expect(dto.decidedAt, DateTime.parse('2026-09-20T08:15:00'));
      expect(dto.decisionNote, 'Grupa za ovaj termin je popunjena.');
    });

    test('fromJson accepts nulls for the nullable fields', () {
      final dto = CourseEnrollmentDto.fromJson({
        'id': 4,
        'studentId': 8,
        'studentName': 'Drugi Student',
        'enrollmentStatus': 'Active',
        'paidUntil': null,
        'decidedAt': null,
        'decisionNote': null,
      });
      expect(dto.paidUntil, isNull);
      expect(dto.decidedAt, isNull);
      expect(dto.decisionNote, isNull);
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

    test('preserves non-ASCII student names', () {
      final entries = parseCourseRanking(jsonEncode([
        {
          'rank': 1,
          'studentId': 3,
          'studentName': 'Čedomir Šešić',
          'averageGrade': 8.0,
          'gradedSubmissions': 1,
        },
      ]));

      expect(entries.single.studentName, 'Čedomir Šešić');
    });

    test('malformed body throws ApiException, not FormatException', () {
      expect(
        () => parseCourseRanking('{"not":"a list"}'),
        throwsA(isA<ApiException>()),
      );
    });
  });

  group('RsvpRequest', () {
    test('toJson carries only the confirm flag (no note field)', () {
      expect(RsvpRequest(confirm: true).toJson(), {'confirm': true});
      expect(RsvpRequest(confirm: false).toJson(), {'confirm': false});
    });
  });

  group('AssignmentSubmissionDto', () {
    test('fromJson and toJson round-trip', () {
      final json = {
        'id': 1,
        'assignmentId': 2,
        'studentId': 3,
        'studentName': 'Student Test',
        'filePath': '/uploads/test.pdf',
        'submittedAt': '2026-09-24T10:00:00.000',
        'grade': 85,
        'feedback': 'Odlično',
      };
      final dto = AssignmentSubmissionDto.fromJson(json);

      expect(dto.id, 1);
      expect(dto.assignmentId, 2);
      expect(dto.studentId, 3);
      expect(dto.studentName, 'Student Test');
      expect(dto.filePath, '/uploads/test.pdf');
      expect(dto.submittedAt, DateTime.parse('2026-09-24T10:00:00.000'));
      expect(dto.grade, 85);
      expect(dto.feedback, 'Odlično');
      expect(dto.toJson(), equals(json));
    });
  });

  group('GradeAssignmentRequest', () {
    test('toJson has no feedback key when null and has it when set', () {
      final requestWithoutFeedback = GradeAssignmentRequest(grade: 85);
      expect(requestWithoutFeedback.toJson(), equals({'grade': 85}));
      expect(requestWithoutFeedback.toJson().containsKey('feedback'), isFalse);

      final requestWithFeedback =
          GradeAssignmentRequest(grade: 85, feedback: 'Odlično');
      expect(
        requestWithFeedback.toJson(),
        equals({'grade': 85, 'feedback': 'Odlično'}),
      );
    });
  });
}
