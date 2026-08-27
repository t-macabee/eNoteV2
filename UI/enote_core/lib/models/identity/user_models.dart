class UserProfileResponse {
  final String role;
  final UserProfile profile;

  UserProfileResponse({required this.role, required this.profile});

  factory UserProfileResponse.fromJson(Map<String, dynamic> json) {
    return UserProfileResponse(
      role: json['role'] as String? ?? '',
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
        dateOfBirth: _parseDate(profile['dateOfBirth']),
        address: profile['address'] != null
            ? UserAddressDto.fromJson(
                Map<String, dynamic>.from(profile['address']))
            : null,
        isActive: profile['isActive'] as bool?,
        enrollmentDate: _parseDate(profile['enrollmentDate']),
        membershipPaidUntil: _parseDate(profile['membershipPaidUntil']),
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
      dateOfBirth: _parseDate(json['dateOfBirth']),
      address: json['address'] != null
          ? UserAddressDto.fromJson(
              Map<String, dynamic>.from(json['address']))
          : null,
      isActive: json['isActive'] as bool?,
      enrollmentDate: _parseDate(json['enrollmentDate']),
      membershipPaidUntil: _parseDate(json['membershipPaidUntil']),
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
      dateOfBirth: _parseDate(json['dateOfBirth']),
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

  InstructorDto({
    required this.id,
    required this.appUserId,
    this.firstName,
    this.lastName,
    this.username,
  });

  factory InstructorDto.fromJson(Map<String, dynamic> json) {
    return InstructorDto(
      id: json['id'] as int? ?? 0,
      appUserId: json['appUserId'] as int? ?? 0,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      username: json['username'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'appUserId': appUserId,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (username != null) 'username': username,
  };
}

class InstructorSearchObject {
  final int? page;
  final int? pageSize;
  final bool? includeTotalCount;
  final String? name;

  InstructorSearchObject({
    this.page,
    this.pageSize,
    this.includeTotalCount,
    this.name,
  });

  Map<String, dynamic> toQueryMap() => {
    if (page != null) 'page': page,
    if (pageSize != null) 'pageSize': pageSize,
    if (includeTotalCount != null) 'includeTotalCount': includeTotalCount,
    if (name != null) 'name': name,
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
