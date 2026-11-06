import 'package:enote_core/enote_core.dart';

class ProfileProvider {
  final ApiClient apiClient;
  int _pictureVersion = 0;

  ProfileProvider({required this.apiClient});

  int get pictureVersion => _pictureVersion;

  Future<UserProfileResponse> getProfile() async {
    final response = await apiClient.get('users/me');
    final data = decodeOrThrow(response);
    _pictureVersion++;
    return UserProfileResponse.fromJson(data);
  }

  Future<void> updateProfile(UpdateProfileRequest request) async {
    final response = await apiClient.put('users/me', body: request.toJson());
    throwIfError(response);
  }

  Future<void> changePassword(ChangePasswordRequest request) async {
    final response = await apiClient.put(
      'users/me/password',
      body: request.toJson(),
    );
    throwIfError(response);
  }

  Future<void> uploadPicture(
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    await uploadUserPicture(apiClient, bytes, fileName, contentType);
    _pictureVersion++;
  }

  Future<void> deletePicture() async {
    final response = await apiClient.delete('users/me/picture');
    throwIfError(response);
    _pictureVersion++;
  }

  String pictureUrl() =>
      userPictureUrl(apiClient, 'me', cacheBuster: _pictureVersion);
}
