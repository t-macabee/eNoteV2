import '../../formatting/formatters.dart';
class UserProfileResponse {
  final String role;
  final String username;
  final String? email;
  final UserProfile profile;
  final bool hasPicture;

  UserProfileResponse({
    required this.role,
    required this.username,
    this.email,
    required this.profile,
    this.hasPicture = false,
  });

  factory UserProfileResponse.fromJson(Map<String, dynamic> json) {
    return UserProfileResponse(
      role: json['role'] as String? ?? '',
      username: json['username'] as String? ?? '',
      email: json['email'] as String?,
      profile: UserProfile.fromJson(json['profile'] as Map<String, dynamic>),
      // Backend serializes positional record as role/username/email/profile/hasPicture (camelCased).
      hasPicture: json['hasPicture'] as bool? ?? false,
    );
  }
}

class UserProfile {
  final int? id;
  final String? firstName;
  final String? lastName;
  final String? username;
  final String? email;
  final DateTime? dateOfBirth;
  final bool? isActive;
  final DateTime? enrollmentDate;
  final DateTime? membershipPaidUntil;
  final String? storeName;
  final String? businessHours;
  final bool? isManager;

  UserProfile({
    this.id,
    this.firstName,
    this.lastName,
    this.username,
    this.email,
    this.dateOfBirth,
    this.isActive,
    this.enrollmentDate,
    this.membershipPaidUntil,
    this.storeName,
    this.businessHours,
    this.isManager,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    final data = json['profile'] as Map<String, dynamic>? ?? json;
    return UserProfile(
      id: data['id'] as int?,
      firstName: data['firstName'] as String?,
      lastName: data['lastName'] as String?,
      username: data['username'] as String?,
      email: data['email'] as String?,
      dateOfBirth: parseDate(data['dateOfBirth']),
      isActive: data['isActive'] as bool?,
      enrollmentDate: parseDate(data['enrollmentDate']),
      membershipPaidUntil: parseDate(data['membershipPaidUntil']),
      storeName: data['storeName'] as String?,
      businessHours: data['businessHours'] as String?,
      isManager: data['isManager'] as bool?,
    );
  }
}

class UserSummaryDto {
  final int id;
  final int appUserId;
  final String? firstName;
  final String? lastName;
  final String? username;
  final bool isActive;

  UserSummaryDto({
    required this.id,
    required this.appUserId,
    this.firstName,
    this.lastName,
    this.username,
    this.isActive = true,
  });

  UserSummaryDto.fromJson(Map<String, dynamic> json)
    : id = json['id'] as int? ?? 0,
      appUserId = json['appUserId'] as int? ?? 0,
      firstName = json['firstName'] as String?,
      lastName = json['lastName'] as String?,
      username = json['username'] as String?,
      isActive = json['isActive'] as bool? ?? true;

  Map<String, dynamic> toJson() => {
    'id': id,
    'appUserId': appUserId,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (username != null) 'username': username,
    'isActive': isActive,
  };
}

class InstructorDto extends UserSummaryDto {
  InstructorDto({
    required super.id,
    required super.appUserId,
    super.firstName,
    super.lastName,
    super.username,
    super.isActive = true,
  });

  InstructorDto.fromJson(super.json) : super.fromJson();
}

class StudentDto extends UserSummaryDto {
  final DateTime? enrollmentDate;
  final DateTime? membershipPaidUntil;

  StudentDto({
    required super.id,
    required super.appUserId,
    super.firstName,
    super.lastName,
    super.username,
    this.enrollmentDate,
    this.membershipPaidUntil,
    super.isActive = true,
  });

  StudentDto.fromJson(super.json)
    : enrollmentDate = parseDate(json['enrollmentDate']),
      membershipPaidUntil = parseDate(json['membershipPaidUntil']),
      super.fromJson();

  @override
  Map<String, dynamic> toJson() => {
    ...super.toJson(),
    if (enrollmentDate != null)
      'enrollmentDate': enrollmentDate!.toIso8601String(),
    if (membershipPaidUntil != null)
      'membershipPaidUntil': membershipPaidUntil!.toIso8601String(),
  };
}

class ShopEmployeeDto {
  final int id;
  final int appUserId;
  final int musicStoreId;
  final String? storeName;
  final String? firstName;
  final String? lastName;
  final String? username;
  final bool isManager;
  final bool isActive;

  ShopEmployeeDto({
    required this.id,
    required this.appUserId,
    required this.musicStoreId,
    this.storeName,
    this.firstName,
    this.lastName,
    this.username,
    required this.isManager,
    required this.isActive,
  });

  factory ShopEmployeeDto.fromJson(Map<String, dynamic> json) {
    return ShopEmployeeDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      musicStoreId: json['musicStoreId'] as int? ?? 0,
      storeName: json['storeName'] as String?,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
      isManager: json['isManager'] as bool? ?? false,
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'appUserId': appUserId,
    'musicStoreId': musicStoreId,
    if (storeName != null) 'storeName': storeName,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (username != null) 'username': username,
    'isManager': isManager,
    'isActive': isActive,
  };
}

