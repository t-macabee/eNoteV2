import 'package:enote_core/enote_core.dart';

class AuthProvider {
  final ApiClient apiClient;

  AuthProvider({required this.apiClient});

  Future<AuthResponse> register(RegisterRequest request) async {
    final response = await apiClient.post('auth/register', body: request.toJson());
    return AuthResponse.fromJson(decodeOrThrow(response));
  }

  Future<void> forgotPassword(String email) async {
    final response = await apiClient.post(
      'auth/forgot-password',
      body: ForgotPasswordRequest(email: email).toJson(),
    );
    decodeOrThrow(response);
  }

  Future<void> resetPassword({
    required String email,
    required String token,
    required String newPassword,
  }) async {
    final response = await apiClient.post(
      'auth/reset-password',
      body: ResetPasswordRequest(
        email: email,
        token: token,
        newPassword: newPassword,
      ).toJson(),
    );
    decodeOrThrow(response);
  }
}
