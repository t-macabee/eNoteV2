import 'package:enote_core/enote_core.dart';

/// Provider seam for the admin "Users" tab's destructive/mutating calls
/// against `admin/users/{id}`, previously issued as raw [ApiClient] calls
/// from [UserGridScreen]'s widget state.
///
/// `remove(id)` (hard delete) is inherited from [BaseProvider] and already
/// does the status check — do not add a fourth delete method here.
class AdminUserProvider extends BaseProvider<UserProfileResponse> {
  AdminUserProvider({required super.apiClient}) : super(endpoint: 'admin/users');

  @override
  UserProfileResponse fromJson(Map<String, dynamic> json) =>
      UserProfileResponse.fromJson(json);

  /// `GET admin/users/{id}`. Throws [ApiException] on error — callers that
  /// want a soft failure (e.g. `_UserDetailsDialog`, which falls back to
  /// basic item info) must catch at the call site, not here.
  Future<UserProfileResponse> getProfile(int id) => getById(id);

  /// `PUT admin/users/{id}/status`.
  Future<void> setStatus(int id, bool isActive) async {
    final response = await apiClient.put(
      '$endpoint/$id/status',
      body: {'isActive': isActive},
    );
    throwIfError(response);
    notifyListeners();
  }

  /// `PUT admin/users/{id}/membership`. Returns [paidUntil] back so callers
  /// can optimistically update local state without a second round-trip.
  Future<DateTime> renewMembership(int id, DateTime paidUntil) async {
    final request = UpdateMembershipRequest(paidUntil: paidUntil);
    final response = await apiClient.put(
      '$endpoint/$id/membership',
      body: request.toJson(),
    );
    throwIfError(response);
    notifyListeners();
    return paidUntil;
  }
}
