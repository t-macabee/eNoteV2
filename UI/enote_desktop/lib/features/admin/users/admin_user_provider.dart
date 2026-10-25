import 'package:enote_core/enote_core.dart';

class AdminUserProvider extends CrudProvider<AdminUserListItemDto> {
  AdminUserProvider({required super.apiClient}) : super(endpoint: 'admin/users');

  @override
  AdminUserListItemDto fromJson(Map<String, dynamic> json) =>
      AdminUserListItemDto.fromJson(json);

  Future<UserProfileResponse> getProfile(int id) async {
    final response = await apiClient.get('$endpoint/$id');
    return UserProfileResponse.fromJson(decodeOrThrow(response));
  }

  Future<void> provision(UserProvisionRequest request) async {
    final response = await apiClient.post(endpoint, body: request.toJson());
    throwIfError(response);
  }

  Future<void> setStatus(int id, bool isActive) async {
    final response = await apiClient.put(
      '$endpoint/$id/status',
      body: {'isActive': isActive},
    );
    throwIfError(response);
    notifyListeners();
  }
}
