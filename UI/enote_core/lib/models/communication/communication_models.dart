import 'package:enote_core/models/shared/enums.dart';

class AnnouncementDto {
  final int id;
  final int? courseId;
  final int? musicStoreId;
  final String title;
  final String content;
  final String? imagePath;
  final AnnouncementScope scope;
  final String? courseName;
  final String? storeName;
  final DateTime publishedAt;

  AnnouncementDto({
    required this.id,
    this.courseId,
    this.musicStoreId,
    required this.title,
    required this.content,
    this.imagePath,
    required this.scope,
    this.courseName,
    this.storeName,
    required this.publishedAt,
  });

  factory AnnouncementDto.fromJson(Map<String, dynamic> json) {
    return AnnouncementDto(
      id: json['id'] as int? ?? 0,
      courseId: json['courseId'] as int?,
      musicStoreId: json['musicStoreId'] as int?,
      title: json['title'] as String? ?? '',
      content: json['content'] as String? ?? '',
      imagePath: json['imagePath'] as String?,
      scope: _parseScope(json['scope']),
      courseName: json['courseName'] as String?,
      storeName: json['storeName'] as String?,
      publishedAt: _parseDate(json['publishedAt']) ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    if (courseId != null) 'courseId': courseId,
    if (musicStoreId != null) 'musicStoreId': musicStoreId,
    'title': title,
    'content': content,
    if (imagePath != null) 'imagePath': imagePath,
    'scope': scope.toJson(),
    if (courseName != null) 'courseName': courseName,
    if (storeName != null) 'storeName': storeName,
    'publishedAt': publishedAt.toIso8601String(),
  };
}

AnnouncementScope _parseScope(dynamic value) {
  if (value is String) return AnnouncementScope.fromJson(value);
  if (value is int) {
    return value == 1 ? AnnouncementScope.course : AnnouncementScope.musicStore;
  }
  return AnnouncementScope.course;
}

class AnnouncementRequest {
  final String title;
  final String content;

  AnnouncementRequest({required this.title, required this.content});

  Map<String, dynamic> toJson() => {
    'title': title,
    'content': content,
  };
}

class AnnouncementSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;

  AnnouncementSearchObject({this.page, this.pageSize, this.includeTotalCount});

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
  };
}

class NotificationDto {
  final int id;
  final int? rentalId;
  final int? lectureId;
  final int? submissionId;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdAt;

  NotificationDto({
    required this.id,
    this.rentalId,
    this.lectureId,
    this.submissionId,
    required this.title,
    required this.body,
    required this.isRead,
    required this.createdAt,
  });

  factory NotificationDto.fromJson(Map<String, dynamic> json) {
    return NotificationDto(
      id: json['id'] as int? ?? 0,
      rentalId: json['rentalId'] as int?,
      lectureId: json['lectureId'] as int?,
      submissionId: json['submissionId'] as int?,
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
      isRead: json['isRead'] as bool? ?? false,
      createdAt: _parseDate(json['createdAt']) ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    if (rentalId != null) 'rentalId': rentalId,
    if (lectureId != null) 'lectureId': lectureId,
    if (submissionId != null) 'submissionId': submissionId,
    'title': title,
    'body': body,
    'isRead': isRead,
    'createdAt': createdAt.toIso8601String(),
  };
}

class NotificationSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;
  final bool? isRead;

  NotificationSearchObject({
    this.page,
    this.pageSize,
    this.includeTotalCount,
    this.isRead,
  });

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
    if (isRead != null) 'isRead': isRead,
  };
}

class NotificationUnreadCountDto {
  final int unreadCount;

  NotificationUnreadCountDto({required this.unreadCount});

  factory NotificationUnreadCountDto.fromJson(Map<String, dynamic> json) {
    return NotificationUnreadCountDto(
      unreadCount: json['unreadCount'] as int? ?? 0,
    );
  }
}

class NotificationPushDto {
  final int? rentalId;
  final int? lectureId;
  final int? submissionId;
  final String title;
  final String body;
  final DateTime createdAt;

  NotificationPushDto({
    this.rentalId,
    this.lectureId,
    this.submissionId,
    required this.title,
    required this.body,
    required this.createdAt,
  });

  factory NotificationPushDto.fromJson(Map<String, dynamic> json) {
    return NotificationPushDto(
      rentalId: json['rentalId'] as int?,
      lectureId: json['lectureId'] as int?,
      submissionId: json['submissionId'] as int?,
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
      createdAt: _parseDate(json['createdAt']) ?? DateTime.now(),
    );
  }
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
