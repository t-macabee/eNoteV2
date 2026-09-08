class StudentEnrollmentDto {
  final int courseId;
  final String courseName;
  final int instructorId;
  final String? instructorName;

  StudentEnrollmentDto({
    required this.courseId,
    required this.courseName,
    required this.instructorId,
    this.instructorName,
  });

  factory StudentEnrollmentDto.fromJson(Map<String, dynamic> json) {
    return StudentEnrollmentDto(
      courseId: json['courseId'] as int? ?? 0,
      courseName: json['courseName'] as String? ?? '',
      instructorId: json['instructorId'] as int? ?? 0,
      instructorName: json['instructorName'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'courseId': courseId,
    'courseName': courseName,
    'instructorId': instructorId,
    if (instructorName != null) 'instructorName': instructorName,
  };
}
