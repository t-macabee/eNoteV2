import 'package:enote_core/enote_core.dart';

class ProfileProvider {
  final ApiClient apiClient;

  ProfileProvider({required this.apiClient});

  Future<UserProfileResponse> getMe() async {
    final response = await apiClient.get('users/me');
    return UserProfileResponse.fromJson(decodeOrThrow(response));
  }

  Future<void> updateMe(UpdateProfileRequest request) async {
    final response = await apiClient.put('users/me', body: request.toJson());
    throwIfError(response);
  }

  Future<void> uploadPicture(
    List<int> bytes,
    String fileName,
    String contentType,
  ) => uploadUserPicture(apiClient, bytes, fileName, contentType);

  Future<void> deletePicture() async {
    final response = await apiClient.delete('users/me/picture');
    throwIfError(response);
  }

  Future<void> changePassword(ChangePasswordRequest request) async {
    final response = await apiClient.put(
      'users/me/password',
      body: request.toJson(),
    );
    throwIfError(response);
  }

  Future<void> deleteProfile(DeleteProfileRequest request) async {
    final response = await apiClient.delete(
      'users/me',
      body: request.toJson(),
    );
    throwIfError(response);
  }
}
