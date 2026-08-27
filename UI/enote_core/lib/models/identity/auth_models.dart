class AuthResponse {
  final int userId;
  final String username;
  final List<String> roles;
  final String token;

  AuthResponse({
    required this.userId,
    required this.username,
    required this.roles,
    required this.token,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      userId: json['userId'] as int? ?? 0,
      username: json['username'] as String? ?? '',
      roles: (json['roles'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
      token: json['token'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'userId': userId,
    'username': username,
    'roles': roles,
    'token': token,
  };
}

class LoginRequest {
  final String username;
  final String password;

  LoginRequest({required this.username, required this.password});

  Map<String, dynamic> toJson() => {
    'username': username,
    'password': password,
  };
}

class RegisterRequest {
  final String username;
  final String email;
  final String password;
  final String? firstName;
  final String? lastName;

  RegisterRequest({
    required this.username,
    required this.email,
    required this.password,
    this.firstName,
    this.lastName,
  });

  Map<String, dynamic> toJson() => {
    'username': username,
    'email': email,
    'password': password,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
  };
}

class ForgotPasswordRequest {
  final String email;

  ForgotPasswordRequest({required this.email});

  Map<String, dynamic> toJson() => {'email': email};
}

class ForgotPasswordResponse {
  final String message;

  ForgotPasswordResponse({required this.message});

  factory ForgotPasswordResponse.fromJson(Map<String, dynamic> json) {
    return ForgotPasswordResponse(message: json['message'] as String? ?? '');
  }

  Map<String, dynamic> toJson() => {'message': message};
}

class ResetPasswordRequest {
  final String email;
  final String token;
  final String newPassword;

  ResetPasswordRequest({
    required this.email,
    required this.token,
    required this.newPassword,
  });

  Map<String, dynamic> toJson() => {
    'email': email,
    'token': token,
    'newPassword': newPassword,
  };
}

class ChangePasswordRequest {
  final String currentPassword;
  final String newPassword;
  final String confirmNewPassword;

  ChangePasswordRequest({
    required this.currentPassword,
    required this.newPassword,
    required this.confirmNewPassword,
  });

  Map<String, dynamic> toJson() => {
    'currentPassword': currentPassword,
    'newPassword': newPassword,
    'confirmNewPassword': confirmNewPassword,
  };
}

class UpdateProfileRequest {
  final String email;
  final String? firstName;
  final String? lastName;
  final DateTime? dateOfBirth;

  UpdateProfileRequest({
    required this.email,
    this.firstName,
    this.lastName,
    this.dateOfBirth,
  });

  Map<String, dynamic> toJson() => {
    'email': email,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (dateOfBirth != null) 'dateOfBirth': dateOfBirth!.toIso8601String(),
  };
}

class UserProvisionRequest {
  final String username;
  final String email;
  final String password;
  final String role;
  final String? firstName;
  final String? lastName;
  final int? musicStoreId;

  UserProvisionRequest({
    required this.username,
    required this.email,
    required this.password,
    required this.role,
    this.firstName,
    this.lastName,
    this.musicStoreId,
  });

  Map<String, dynamic> toJson() => {
    'username': username,
    'email': email,
    'password': password,
    'role': role,
    if (firstName != null) 'firstName': firstName,
    if (lastName != null) 'lastName': lastName,
    if (musicStoreId != null) 'musicStoreId': musicStoreId,
  };
}

class UpdateMembershipRequest {
  final DateTime? paidUntil;

  UpdateMembershipRequest({this.paidUntil});

  Map<String, dynamic> toJson() => {
    if (paidUntil != null) 'paidUntil': paidUntil!.toIso8601String(),
  };
}
