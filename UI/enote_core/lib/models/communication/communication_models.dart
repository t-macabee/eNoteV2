import 'package:enote_core/models/shared/enums.dart';
import '../../formatting/formatters.dart';

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
      scope: AnnouncementScope.fromDynamic(json['scope']),
      courseName: json['courseName'] as String?,
      storeName: json['storeName'] as String?,
      publishedAt: parseDate(json['publishedAt']) ?? DateTime.now(),
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

class AnnouncementRequest {
  final String title;
  final String content;

  AnnouncementRequest({required this.title, required this.content});

  Map<String, dynamic> toJson() => {
    'title': title,
    'content': content,
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
      createdAt: parseDate(json['createdAt']) ?? DateTime.now(),
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

class EventDto {
  final int id;
  final String title;
  final String description;
  final DateTime startsAt;
  final DateTime? endsAt;
  final int? addressId;
  final String? addressStreet;
  final String? addressCity;
  final int? courseId;
  final String? courseName;
  final int? instructorId;

  EventDto({
    required this.id,
    required this.title,
    required this.description,
    required this.startsAt,
    this.endsAt,
    this.addressId,
    this.addressStreet,
    this.addressCity,
    this.courseId,
    this.courseName,
    this.instructorId,
  });

  factory EventDto.fromJson(Map<String, dynamic> json) {
    return EventDto(
      id: json['id'] as int? ?? 0,
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      startsAt: parseDate(json['startsAt']) ?? DateTime.now(),
      endsAt: parseDate(json['endsAt']),
      addressId: json['addressId'] as int?,
      addressStreet: json['addressStreet'] as String?,
      addressCity: json['addressCity'] as String?,
      courseId: json['courseId'] as int?,
      courseName: json['courseName'] as String?,
      instructorId: json['instructorId'] as int?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'title': title,
    'description': description,
    'startsAt': startsAt.toIso8601String(),
    if (endsAt != null) 'endsAt': endsAt!.toIso8601String(),
    if (addressId != null) 'addressId': addressId,
    if (addressStreet != null) 'addressStreet': addressStreet,
    if (addressCity != null) 'addressCity': addressCity,
    if (courseId != null) 'courseId': courseId,
    if (courseName != null) 'courseName': courseName,
    if (instructorId != null) 'instructorId': instructorId,
  };
}

class EventRequest {
  final String title;
  final String description;
  final DateTime startsAt;
  final DateTime? endsAt;
  final int? addressId;
  final int? courseId;
  final int? instructorId;

  EventRequest({
    required this.title,
    required this.description,
    required this.startsAt,
    this.endsAt,
    this.addressId,
    this.courseId,
    this.instructorId,
  });

  Map<String, dynamic> toJson() => {
    'title': title,
    'description': description,
    'startsAt': startsAt.toIso8601String(),
    if (endsAt != null) 'endsAt': endsAt!.toIso8601String(),
    if (addressId != null) 'addressId': addressId,
    if (courseId != null) 'courseId': courseId,
    if (instructorId != null) 'instructorId': instructorId,
  };
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
      createdAt: parseDate(json['createdAt']) ?? DateTime.now(),
    );
  }
}

