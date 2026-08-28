import '../../formatting/formatters.dart';
class CourseDto {
  final int id;
  final int instructorId;
  final String name;
  final String? description;
  final bool isPublished;
  final DateTime? startDate;
  final DateTime? endDate;
  final double price;
  final int enrolledCount;

  CourseDto({
    required this.id,
    required this.instructorId,
    required this.name,
    this.description,
    required this.isPublished,
    this.startDate,
    this.endDate,
    required this.price,
    required this.enrolledCount,
  });

  factory CourseDto.fromJson(Map<String, dynamic> json) {
    return CourseDto(
      id: json['id'] as int? ?? 0,
      instructorId: json['instructorId'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      description: json['description'] as String?,
      isPublished: json['isPublished'] as bool? ?? false,
      startDate: parseDate(json['startDate']),
      endDate: parseDate(json['endDate']),
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      enrolledCount: json['enrolledCount'] as int? ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'instructorId': instructorId,
    'name': name,
    if (description != null) 'description': description,
    'isPublished': isPublished,
    if (startDate != null) 'startDate': startDate!.toIso8601String(),
    if (endDate != null) 'endDate': endDate!.toIso8601String(),
    'price': price,
    'enrolledCount': enrolledCount,
  };
}

class CourseRequest {
  final String name;
  final String? description;
  final double price;
  final DateTime? startDate;
  final DateTime? endDate;
  final bool isPublished;

  CourseRequest({
    required this.name,
    this.description,
    required this.price,
    this.startDate,
    this.endDate,
    required this.isPublished,
  });

  Map<String, dynamic> toJson() => {
    'name': name,
    if (description != null) 'description': description,
    'price': price,
    if (startDate != null) 'startDate': startDate!.toIso8601String(),
    if (endDate != null) 'endDate': endDate!.toIso8601String(),
    'isPublished': isPublished,
  };
}

class CourseRankingEntryDto {
  final int rank;
  final int studentId;
  final String studentName;
  final double? averageGrade;
  final int gradedSubmissions;

  CourseRankingEntryDto({
    required this.rank,
    required this.studentId,
    required this.studentName,
    this.averageGrade,
    required this.gradedSubmissions,
  });

  factory CourseRankingEntryDto.fromJson(Map<String, dynamic> json) {
    return CourseRankingEntryDto(
      rank: json['rank'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      averageGrade: (json['averageGrade'] as num?)?.toDouble(),
      gradedSubmissions: json['gradedSubmissions'] as int? ?? 0,
    );
  }
}

