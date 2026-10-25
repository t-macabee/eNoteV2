import 'package:enote_core/enote_core.dart';

class AdminUserProvider extends CrudProvider<UserProfileResponse> {
  AdminUserProvider({required super.apiClient}) : super(endpoint: 'admin/users');

  @override
  UserProfileResponse fromJson(Map<String, dynamic> json) =>
      UserProfileResponse.fromJson(json);

  Future<UserProfileResponse> getProfile(int id) => getById(id);

  Future<PagedResult<AdminUserListItemDto>> getUsers(
    Map<String, dynamic> params,
  ) async {
    final response = await apiClient.get(endpoint, queryParams: params);
    throwIfError(response);
    return parsePage(response, AdminUserListItemDto.fromJson, params: params);
  }

  Future<int> provision(UserProvisionRequest request) async {
    final response = await apiClient.post(endpoint, body: request.toJson());
    final data = decodeOrThrow(response);
    return data['userId'] as int? ?? data['id'] as int? ?? 0;
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
