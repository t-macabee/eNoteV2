import '../academic/assignment_models.dart';
import '../academic/course_models.dart';
import '../academic/lecture_models.dart';
import '../rentals/instrument_rental_models.dart';
import '../rentals/rental_debt_dto.dart';

class StudentHomeDto {
  final LectureDto? nextLecture;
  final List<AssignmentDto> openAssignments;
  final List<InstrumentRentalDto> currentRentals;
  final List<CourseDto> tuitionDueCourses;
  final RentalDebtDto debt;

  StudentHomeDto({
    this.nextLecture,
    this.openAssignments = const [],
    this.currentRentals = const [],
    this.tuitionDueCourses = const [],
    required this.debt,
  });

  factory StudentHomeDto.fromJson(Map<String, dynamic> json) {
    final rawNextLecture = json['nextLecture'] as Map<String, dynamic>?;
    final rawAssignments =
        json['openAssignments'] as List<dynamic>? ?? const [];
    final rawRentals = json['currentRentals'] as List<dynamic>? ?? const [];
    final rawCourses = json['tuitionDueCourses'] as List<dynamic>? ?? const [];
    final rawDebt = json['debt'] as Map<String, dynamic>? ?? const {};

    return StudentHomeDto(
      nextLecture: rawNextLecture == null
          ? null
          : LectureDto.fromJson(rawNextLecture),
      openAssignments: [
        for (final item in rawAssignments)
          AssignmentDto.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      currentRentals: [
        for (final item in rawRentals)
          InstrumentRentalDto.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      tuitionDueCourses: [
        for (final item in rawCourses)
          CourseDto.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      debt: RentalDebtDto.fromJson(rawDebt),
    );
  }
}
