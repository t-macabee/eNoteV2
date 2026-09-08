import 'package:enote_core/enote_core.dart';

/// Wraps the logged-in user's own `users/me` endpoints for [ProfileDialog]
/// and its two sub-forms.
///
/// Not a `BaseProvider<T>`, for the same reason as `UserProvisionService`:
/// `users/me` is a single-record resource, not the paged
/// list/search/insert/update/remove shape `BaseProvider` assumes.
class ProfileProvider {
  final ApiClient apiClient;

  ProfileProvider({required this.apiClient});

  /// `GET users/me`. Throws [ApiException] on error — callers that want a
  /// soft failure must catch at the call site.
  Future<UserProfileResponse> getProfile() async {
    final response = await apiClient.get('users/me');
    final data = decodeOrThrow(response);
    return UserProfileResponse.fromJson(data);
  }

  /// `PUT users/me`.
  Future<void> updateProfile(UpdateProfileRequest request) async {
    final response = await apiClient.put('users/me', body: request.toJson());
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

  /// `PUT users/me/picture` (multipart `file`, 5 MB limit server-side).
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

  /// Read path for `GET users/me/picture`: an origin-relative URL carrying
  /// the client's bearer headers via [networkImageOrPlaceholder].
  ///
  /// Returns e.g. `/api/v1/users/me/picture` (plus `?v=` cache-buster when
  /// [cacheBuster] is given) so the image reloads after upload/delete.
  String pictureUrl({int? cacheBuster}) =>
      pictureUrlForUser(apiClient, 'me', cacheBuster: cacheBuster);

  /// Cross-user read path for `GET users/{id}/picture` (Part C). [userId] is
  /// the `AppUser` id. A 404 (no picture / not visible) is normal — callers
  /// must fall back to initials, never an error banner.
  static String pictureUrlForUser(
    ApiClient client,
    Object userId, {
    int? cacheBuster,
  }) =>
      userPictureUrl(client, userId, cacheBuster: cacheBuster);
}
