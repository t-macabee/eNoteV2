import '../../formatting/formatters.dart';
class UserProfileResponse {
  final String role;
  final String username;
  final String? email;
  final UserProfile profile;

  UserProfileResponse({
    required this.role,
    required this.username,
    this.email,
    required this.profile,
  });

  factory UserProfileResponse.fromJson(Map<String, dynamic> json) {
    return UserProfileResponse(
      role: json['role'] as String? ?? '',
      username: json['username'] as String? ?? '',
      email: json['email'] as String?,
      profile: UserProfile.fromJson(json['profile'] as Map<String, dynamic>),
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
  final UserAddressDto? address;
  final bool? isActive;
  final DateTime? enrollmentDate;
  final DateTime? membershipPaidUntil;
  final String? storeName;
  final String? businessHours;

  UserProfile({
    this.id,
    this.firstName,
    this.lastName,
    this.username,
    this.email,
    this.dateOfBirth,
    this.address,
    this.isActive,
    this.enrollmentDate,
    this.membershipPaidUntil,
    this.storeName,
    this.businessHours,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    final profile = json['profile'] as Map<String, dynamic>?;
    if (profile != null) {
      return UserProfile(
        id: profile['id'] as int?,
        firstName: profile['firstName'] as String?,
        lastName: profile['lastName'] as String?,
        username: profile['username'] as String?,
        email: profile['email'] as String?,
        dateOfBirth: parseDate(profile['dateOfBirth']),
        address: profile['address'] != null
            ? UserAddressDto.fromJson(
                Map<String, dynamic>.from(profile['address']))
            : null,
        isActive: profile['isActive'] as bool?,
        enrollmentDate: parseDate(profile['enrollmentDate']),
        membershipPaidUntil: parseDate(profile['membershipPaidUntil']),
        storeName: profile['storeName'] as String?,
        businessHours: profile['businessHours'] as String?,
      );
    }
    return UserProfile(
      id: json['id'] as int?,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
      email: json['email'] as String?,
      dateOfBirth: parseDate(json['dateOfBirth']),
      address: json['address'] != null
          ? UserAddressDto.fromJson(
              Map<String, dynamic>.from(json['address']))
          : null,
      isActive: json['isActive'] as bool?,
      enrollmentDate: parseDate(json['enrollmentDate']),
      membershipPaidUntil: parseDate(json['membershipPaidUntil']),
      storeName: json['storeName'] as String?,
      businessHours: json['businessHours'] as String?,
    );
  }
}

class UserAddressDto {
  final String city;
  final String street;
  final String number;

  UserAddressDto({
    required this.city,
    required this.street,
    required this.number,
  });

  factory UserAddressDto.fromJson(Map<String, dynamic> json) {
    return UserAddressDto(
      city: json['city'] as String? ?? '',
      street: json['street'] as String? ?? '',
      number: json['number'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'city': city,
    'street': street,
    'number': number,
  };
}

class UserIdentityDto {
  final int? id;
  final String? username;
  final String? firstName;
  final String? lastName;
  final UserAddressDto? address;
  final DateTime? dateOfBirth;
  final bool hasPicture;
  final bool isActive;

  UserIdentityDto({
    this.id,
    this.username,
    this.firstName,
    this.lastName,
    this.address,
    this.dateOfBirth,
    this.hasPicture = false,
    this.isActive = false,
  });

  factory UserIdentityDto.fromJson(Map<String, dynamic> json) {
    return UserIdentityDto(
      id: json['id'] as int?,
      username: json['username'] as String?,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      address: json['address'] != null
          ? UserAddressDto.fromJson(
              Map<String, dynamic>.from(json['address']))
          : null,
      dateOfBirth: parseDate(json['dateOfBirth']),
      hasPicture: json['hasPicture'] as bool? ?? false,
      isActive: json['isActive'] as bool? ?? false,
    );
  }
}

class InstructorDto {
  final int id;
  final int appUserId;
  final String? firstName;
  final String? lastName;
  final String? username;
  final bool isActive;

  InstructorDto({
    required this.id,
    required this.appUserId,
    this.firstName,
    this.lastName,
    this.username,
    this.isActive = true,
  });

  factory InstructorDto.fromJson(Map<String, dynamic> json) {
    return InstructorDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'appUserId': appUserId,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (username != null) 'username': username,
    'isActive': isActive,
  };
}

class StudentDto {
  final int id;
  final int appUserId;
  final String? firstName;
  final String? lastName;
  final String? username;
  final DateTime? enrollmentDate;
  final DateTime? membershipPaidUntil;
  final bool isActive;

  StudentDto({
    required this.id,
    required this.appUserId,
    this.firstName,
    this.lastName,
    this.username,
    this.enrollmentDate,
    this.membershipPaidUntil,
    this.isActive = true,
  });

  factory StudentDto.fromJson(Map<String, dynamic> json) {
    return StudentDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
      enrollmentDate: parseDate(json['enrollmentDate']),
      membershipPaidUntil: parseDate(json['membershipPaidUntil']),
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'appUserId': appUserId,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (username != null) 'username': username,
    'isActive': isActive,
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

  String? get musicStoreName => storeName;

  factory ShopEmployeeDto.fromJson(Map<String, dynamic> json) {
    return ShopEmployeeDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      musicStoreId: json['musicStoreId'] as int? ?? 0,
      storeName: json['storeName'] as String? ?? json['musicStoreName'] as String?,
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

