import 'package:enote_core/enote_core.dart';

class ProfileProvider {
  final ApiClient apiClient;

  ProfileProvider({required this.apiClient});

  Future<UserProfileResponse> getProfile() async {
    final response = await apiClient.get('users/me');
    final data = decodeOrThrow(response);
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
  ) => uploadUserPicture(apiClient, bytes, fileName, contentType);

  Future<void> deletePicture() async {
    final response = await apiClient.delete('users/me/picture');
    throwIfError(response);
  }

  String pictureUrl({int? cacheBuster}) =>
      userPictureUrl(apiClient, 'me', cacheBuster: cacheBuster);
}
