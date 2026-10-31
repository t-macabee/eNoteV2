import '../shared/enums.dart';

class StudentDirectoryEntryDto {
  final int id;
  final int appUserId;
  final String? firstName;
  final String? lastName;
  final String? username;

  StudentDirectoryEntryDto({
    required this.id,
    required this.appUserId,
    this.firstName,
    this.lastName,
    this.username,
  });

  factory StudentDirectoryEntryDto.fromJson(Map<String, dynamic> json) {
    return StudentDirectoryEntryDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
    );
  }
}

class StudentProfileDto {
  final int id;
  final int appUserId;
  final String? firstName;
  final String? lastName;
  final String? username;
  final List<StudentCourseGradeDto> courses;

  StudentProfileDto({
    required this.id,
    required this.appUserId,
    this.firstName,
    this.lastName,
    this.username,
    this.courses = const [],
  });

  factory StudentProfileDto.fromJson(Map<String, dynamic> json) {
    final rawCourses = json['courses'] as List<dynamic>? ?? const [];
    return StudentProfileDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
      courses: [
        for (final item in rawCourses)
          StudentCourseGradeDto.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
    );
  }
}

class StudentCourseGradeDto {
  final int courseId;
  final String courseName;
  final String? instructorName;
  final EnrollmentStatus enrollmentStatus;
  final double? averageGrade;
  final int gradedSubmissions;

  StudentCourseGradeDto({
    required this.courseId,
    required this.courseName,
    this.instructorName,
    required this.enrollmentStatus,
    this.averageGrade,
    required this.gradedSubmissions,
  });

  factory StudentCourseGradeDto.fromJson(Map<String, dynamic> json) {
    return StudentCourseGradeDto(
      courseId: json['courseId'] as int? ?? 0,
      courseName: json['courseName'] as String? ?? '',
      instructorName: json['instructorName'] as String?,
      enrollmentStatus:
          EnrollmentStatus.fromJson(json['enrollmentStatus'] as String?),
      averageGrade: (json['averageGrade'] as num?)?.toDouble(),
      gradedSubmissions: json['gradedSubmissions'] as int? ?? 0,
    );
  }
}
