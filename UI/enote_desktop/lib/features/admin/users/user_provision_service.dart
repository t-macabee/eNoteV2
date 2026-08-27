import 'dart:convert';

import 'package:enote_core/enote_core.dart';

/// Thin wrapper around `POST admin/users` / `GET admin/users/{id}`.
///
/// Not a `BaseProvider<T>`: that base class assumes the paged
/// `ReferenceDataCrudService` shape (list/search/insert/update/remove over
/// `PagedResult<T>`), and `AdminUsersController` has none of that — only a
/// single-record provision endpoint and a single-record lookup.
class UserProvisionService {
  final ApiClient apiClient;

  UserProvisionService({required this.apiClient});

  /// Provisions (or re-provisions, if the username already exists) a user.
  /// Returns the new/updated user's id.
  Future<int> provision(UserProvisionRequest request) async {
    final response = await apiClient.post('admin/users', body: request.toJson());

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return data['userId'] as int? ?? data['id'] as int? ?? 0;
  }

  Future<UserProfileResponse> getById(int id) async {
    final response = await apiClient.get('admin/users/$id');

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return UserProfileResponse.fromJson(data);
  }
}
