import '../../formatting/formatters.dart';
class AssignmentDto {
  final int id;
  final int lectureId;
  final String title;
  final String description;
  final DateTime dueAt;

  AssignmentDto({
    required this.id,
    required this.lectureId,
    required this.title,
    required this.description,
    required this.dueAt,
  });

  factory AssignmentDto.fromJson(Map<String, dynamic> json) {
    return AssignmentDto(
      id: json['id'] as int? ?? 0,
      lectureId: json['lectureId'] as int? ?? 0,
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      dueAt: parseDate(json['dueAt']) ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'lectureId': lectureId,
    'title': title,
    'description': description,
    'dueAt': toUtcIso(dueAt),
  };
}

class AssignmentRequest {
  final String title;
  final String description;
  final DateTime dueAt;

  AssignmentRequest({
    required this.title,
    required this.description,
    required this.dueAt,
  });

  Map<String, dynamic> toJson() => {
    'title': title,
    'description': description,
    'dueAt': toUtcIso(dueAt),
  };
}

class AssignmentSubmissionDto {
  final int id;
  final int assignmentId;
  final int studentId;
  final String? studentName;
  final String? filePath;
  final DateTime? submittedAt;
  final int? grade;
  final String? feedback;

  AssignmentSubmissionDto({
    required this.id,
    required this.assignmentId,
    required this.studentId,
    this.studentName,
    this.filePath,
    this.submittedAt,
    this.grade,
    this.feedback,
  });

  factory AssignmentSubmissionDto.fromJson(Map<String, dynamic> json) {
    return AssignmentSubmissionDto(
      id: json['id'] as int? ?? 0,
      assignmentId: json['assignmentId'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String?,
      filePath: json['filePath'] as String?,
      submittedAt: parseDate(json['submittedAt']),
      grade: json['grade'] as int?,
      feedback: json['feedback'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'assignmentId': assignmentId,
    'studentId': studentId,
    if (studentName != null) 'studentName': studentName,
    if (filePath != null) 'filePath': filePath,
    if (submittedAt != null) 'submittedAt': submittedAt!.toIso8601String(),
    if (grade != null) 'grade': grade,
    if (feedback != null) 'feedback': feedback,
  };
}

class GradeAssignmentRequest {
  final int grade;
  final String? feedback;

  GradeAssignmentRequest({required this.grade, this.feedback});

  Map<String, dynamic> toJson() => {
    'grade': grade,
    if (feedback != null) 'feedback': feedback,
  };
}

