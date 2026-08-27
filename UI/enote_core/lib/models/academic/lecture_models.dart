import '../shared/enums.dart';

class LectureDto {
  final int id;
  final String name;
  final String location;
  final LectureType lectureType;
  final LectureStatus lectureStatus;
  final bool isCancelled;
  final DateTime lectureTime;
  final int duration;
  final int? capacity;
  final int attendeeCount;

  LectureDto({
    required this.id,
    required this.name,
    required this.location,
    required this.lectureType,
    required this.lectureStatus,
    required this.isCancelled,
    required this.lectureTime,
    required this.duration,
    this.capacity,
    required this.attendeeCount,
  });

  factory LectureDto.fromJson(Map<String, dynamic> json) {
    final typeValue = json['lectureType'];
    final statusValue = json['lectureStatus'];
    return LectureDto(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      location: json['location'] as String? ?? '',
      lectureType: typeValue is String
          ? LectureType.fromJson(typeValue)
          : LectureType.theoretical,
      lectureStatus: statusValue is int
          ? LectureStatus.fromJson(statusValue)
          : LectureStatus.scheduled,
      isCancelled: json['isCancelled'] as bool? ?? false,
      lectureTime: _parseDate(json['lectureTime']) ?? DateTime.now(),
      duration: json['duration'] as int? ?? 0,
      capacity: json['capacity'] as int?,
      attendeeCount: json['attendeeCount'] as int? ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'location': location,
    'lectureType': lectureType.toJson(),
    'lectureStatus': lectureStatus.toJson(),
    'isCancelled': isCancelled,
    'lectureTime': lectureTime.toIso8601String(),
    'duration': duration,
    if (capacity != null) 'capacity': capacity,
    'attendeeCount': attendeeCount,
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

  LectureCreateRequest({
    required this.name,
    required this.location,
    required this.lectureType,
    required this.lectureTime,
    required this.duration,
    this.capacity,
    required this.courseId,
  });

  Map<String, dynamic> toJson() => {
    'name': name,
    'location': location,
    'lectureType': lectureType.toJson(),
    'lectureTime': lectureTime.toIso8601String(),
    'duration': duration,
    if (capacity != null) 'capacity': capacity,
    'courseId': courseId,
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
    'lectureTime': lectureTime.toIso8601String(),
    'duration': duration,
    if (capacity != null) 'capacity': capacity,
  };
}

class LectureSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;
  final int? courseId;
  final String? name;
  final LectureType? lectureType;
  final DateTime? from;
  final DateTime? to;

  LectureSearchObject({
    this.page,
    this.pageSize,
    this.includeTotalCount,
    this.courseId,
    this.name,
    this.lectureType,
    this.from,
    this.to,
  });

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
    if (courseId != null) 'courseId': courseId,
    if (name != null) 'name': name,
    if (lectureType != null) 'lectureType': lectureType!.toJson(),
    if (from != null) 'from': from!.toIso8601String(),
    if (to != null) 'to': to!.toIso8601String(),
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
    final statusValue = json['attendanceStatus'];
    return AttendanceDto(
      id: json['id'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      attendanceStatus: statusValue is int
          ? AttendanceStatus.fromJson(statusValue)
          : AttendanceStatus.pending,
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
  final String? note;

  RsvpRequest({required this.confirm, this.note});

  Map<String, dynamic> toJson() => {
    'confirm': confirm,
    if (note != null) 'note': note,
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

class AttendanceSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;

  AttendanceSearchObject({this.page, this.pageSize, this.includeTotalCount});

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
