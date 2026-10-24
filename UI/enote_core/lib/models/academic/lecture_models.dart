import '../shared/enums.dart';
import '../../formatting/formatters.dart';

class LectureDto {
  final int id;
  final int? studentId;
  final String? studentName;
  final String name;
  final String location;
  final LectureType lectureType;
  final LectureStatus lectureStatus;
  final bool isCancelled;
  final String? cancellationReason;
  final DateTime? cancelledAt;
  final DateTime lectureTime;
  final int duration;
  final int? capacity;
  final int attendeeCount;
  final AttendanceStatus? myAttendanceStatus;

  LectureDto({
    required this.id,
    this.studentId,
    this.studentName,
    required this.name,
    required this.location,
    required this.lectureType,
    required this.lectureStatus,
    required this.isCancelled,
    this.cancellationReason,
    this.cancelledAt,
    required this.lectureTime,
    required this.duration,
    this.capacity,
    required this.attendeeCount,
    this.myAttendanceStatus,
  });

  bool get isIndividual => studentId != null;

  factory LectureDto.fromJson(Map<String, dynamic> json) {
    return LectureDto(
      id: json['id'] as int? ?? 0,
      studentId: json['studentId'] as int?,
      studentName: json['studentName'] as String?,
      name: json['name'] as String? ?? '',
      location: json['location'] as String? ?? '',
      lectureType: LectureType.fromJson(json['lectureType'] as String?),
      lectureStatus: LectureStatus.fromJson(json['lectureStatus'] as String?),
      isCancelled: json['isCancelled'] as bool? ?? false,
      cancellationReason: json['cancellationReason'] as String?,
      cancelledAt: parseDate(json['cancelledAt']),
      lectureTime: parseDate(json['lectureTime']) ?? DateTime.now(),
      duration: json['duration'] as int? ?? 0,
      capacity: json['capacity'] as int?,
      attendeeCount: json['attendeeCount'] as int? ?? 0,
      myAttendanceStatus: json['myAttendanceStatus'] == null
          ? null
          : AttendanceStatus.fromJson(json['myAttendanceStatus'] as String?),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    if (studentId != null) 'studentId': studentId,
    if (studentName != null) 'studentName': studentName,
    'name': name,
    'location': location,
    'lectureType': lectureType.toJson(),
    'lectureStatus': lectureStatus.toJson(),
    'isCancelled': isCancelled,
    if (cancellationReason != null) 'cancellationReason': cancellationReason,
    if (cancelledAt != null) 'cancelledAt': toUtcIso(cancelledAt!),
    'lectureTime': toUtcIso(lectureTime),
    'duration': duration,
    if (capacity != null) 'capacity': capacity,
    'attendeeCount': attendeeCount,
    if (myAttendanceStatus != null)
      'myAttendanceStatus': myAttendanceStatus!.toJson(),
  };
}

class LectureCreateRequest {
  final String name;
  final String location;
  final LectureType lectureType;
  final DateTime lectureTime;
  final int duration;
  final int? capacity;
  final int courseId;
  final int? studentId;

  LectureCreateRequest({
    required this.name,
    required this.location,
    required this.lectureType,
    required this.lectureTime,
    required this.duration,
    this.capacity,
    required this.courseId,
    this.studentId,
  });

  Map<String, dynamic> toJson() => {
    'name': name,
    'location': location,
    'lectureType': lectureType.toJson(),
    'lectureTime': toUtcIso(lectureTime),
    'duration': duration,
    if (capacity != null) 'capacity': capacity,
    'courseId': courseId,
    if (studentId != null) 'studentId': studentId,
  };
}

class LectureUpdateRequest {
  final String name;
  final String location;
  final DateTime lectureTime;
  final int duration;
  final int? capacity;

  LectureUpdateRequest({
    required this.name,
    required this.location,
    required this.lectureTime,
    required this.duration,
    this.capacity,
  });

  Map<String, dynamic> toJson() => {
    'name': name,
    'location': location,
    'lectureTime': toUtcIso(lectureTime),
    'duration': duration,
    if (capacity != null) 'capacity': capacity,
  };
}

class AttendanceDto {
  final int id;
  final int studentId;
  final String studentName;
  final AttendanceStatus attendanceStatus;

  AttendanceDto({
    required this.id,
    required this.studentId,
    required this.studentName,
    required this.attendanceStatus,
  });

  factory AttendanceDto.fromJson(Map<String, dynamic> json) {
    return AttendanceDto(
      id: json['id'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      attendanceStatus:
          AttendanceStatus.fromJson(json['attendanceStatus'] as String?),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'studentId': studentId,
    'studentName': studentName,
    'attendanceStatus': attendanceStatus.toJson(),
  };
}

class MarkAttendanceRequest {
  final int studentId;
  final AttendanceStatus attendanceStatus;

  MarkAttendanceRequest({
    required this.studentId,
    required this.attendanceStatus,
  });

  Map<String, dynamic> toJson() => {
    'studentId': studentId,
    'attendanceStatus': attendanceStatus.toJson(),
  };
}

class RsvpRequest {
  final bool confirm;

  RsvpRequest({required this.confirm});

  Map<String, dynamic> toJson() => {
    'confirm': confirm,
  };
}

class RsvpResponse {
  final int lectureId;
  final int studentId;
  final bool confirmed;

  RsvpResponse({
    required this.lectureId,
    required this.studentId,
    required this.confirmed,
  });

  factory RsvpResponse.fromJson(Map<String, dynamic> json) {
    return RsvpResponse(
      lectureId: json['lectureId'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      confirmed: json['confirmed'] as bool? ?? false,
    );
  }
}

