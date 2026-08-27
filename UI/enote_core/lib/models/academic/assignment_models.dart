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
      dueAt: _parseDate(json['dueAt']) ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'lectureId': lectureId,
    'title': title,
    'description': description,
    'dueAt': dueAt.toIso8601String(),
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
    'dueAt': dueAt.toIso8601String(),
  };
}

class AssignmentSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;

  AssignmentSearchObject({this.page, this.pageSize, this.includeTotalCount});

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
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

  AssignmentSubmissionDto({
    required this.id,
    required this.assignmentId,
    required this.studentId,
    this.studentName,
    this.filePath,
    this.submittedAt,
    this.grade,
  });

  factory AssignmentSubmissionDto.fromJson(Map<String, dynamic> json) {
    return AssignmentSubmissionDto(
      id: json['id'] as int? ?? 0,
      assignmentId: json['assignmentId'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String?,
      filePath: json['filePath'] as String?,
      submittedAt: _parseDate(json['submittedAt']),
      grade: json['grade'] as int?,
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
  };
}

class GradeAssignmentRequest {
  final int grade;

  GradeAssignmentRequest({required this.grade});

  Map<String, dynamic> toJson() => {'grade': grade};
}

class SubmissionSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;

  SubmissionSearchObject({this.page, this.pageSize, this.includeTotalCount});

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
  };
}

DateTime? _parseDate(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  if (value is String) {
    try {
      return DateTime.parse(value);
    } catch (_) {
      return null;
    }
  }
  return null;
}
