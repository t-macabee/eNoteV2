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
}
