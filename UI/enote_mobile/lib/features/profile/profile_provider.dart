import 'package:enote_core/enote_core.dart';

/// Wraps the logged-in student's own `users/me` endpoints for the profile
/// screens (S30–S32).
///
/// Plain class over [ApiClient]; not a paged provider because `users/me`
/// is a single-record resource.
class ProfileProvider {
  final ApiClient apiClient;

  ProfileProvider({required this.apiClient});

  /// `GET users/me`.
  Future<UserProfileResponse> getMe() async {
    final response = await apiClient.get('users/me');
    return UserProfileResponse.fromJson(decodeOrThrow(response));
  }

  /// `PUT users/me`.
  Future<void> updateMe(UpdateProfileRequest request) async {
    final response = await apiClient.put('users/me', body: request.toJson());
    throwIfError(response);
  }

  /// `PUT users/me/picture` (multipart part `file`).
  Future<void> uploadPicture(
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    final response = await apiClient.putMultipart(
      'users/me/picture',
      bytes: bytes,
      fileName: fileName,
      contentType: contentType,
    );
    throwIfError(response);
  }

  /// `DELETE users/me/picture`.
  Future<void> deletePicture() async {
    final response = await apiClient.delete('users/me/picture');
    throwIfError(response);
  }

  /// `PUT users/me/password`.
  Future<void> changePassword(ChangePasswordRequest request) async {
    final response = await apiClient.put(
      'users/me/password',
      body: request.toJson(),
    );
    throwIfError(response);
  }
}
