import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../api/api_client.dart';
import '../api/api_response.dart';
import '../models/identity/auth_models.dart';
import '../paging/paged_result.dart';

/// Read-only provider surface: search/getById only.
///
/// Used by providers whose screens demonstrably call only the narrow subset
/// (e.g. AdminCourse, Instructor/Student/StoreEmployee lookup for the Users
/// union view, shop address/type lookups, catalog). Providers that need
/// insert/update/remove/uploadImage must extend [CrudProvider] (or
/// [BaseProvider] when they also need [BaseProvider.createDelegatedUser]).
abstract class ReadOnlyProvider<T> with ChangeNotifier {
  final ApiClient apiClient;
  final String endpoint;

  ReadOnlyProvider({
    required this.apiClient,
    required this.endpoint,
  });

  T fromJson(Map<String, dynamic> json);

  Future<PagedResult<T>> getPage({Map<String, dynamic>? params}) async {
    final response = await apiClient.get(endpoint, queryParams: params);
    throwIfError(response);

    final result = parsePage<T>(response, fromJson, params: params);
    return result;
  }

  @protected
  PagedResult<R> parsePage<R>(
    http.Response response,
    R Function(Map<String, dynamic>) fromJsonT, {
    Map<String, dynamic>? params,
  }) {
    // Decodes via decodeOrThrow so a 200 with an empty, HTML, or otherwise
    // malformed body raises the distinct parse-failure ApiException instead
    // of a raw TypeError/FormatException.
    final data = decodeOrThrow(response);
    final items = (data['items'] as List<dynamic>? ?? []);
    final page = data['page'] as int? ?? params?['page'] as int? ?? 1;
    final pageSize = data['pageSize'] as int? ?? params?['pageSize'] as int? ?? 20;
    final totalCount = data['totalCount'] as int?;
    return PagedResult<R>(
      items: items.map((e) => fromJsonT(Map<String, dynamic>.from(e))).toList(),
      page: page,
      pageSize: pageSize,
      totalCount: totalCount,
    );
  }

  Future<T> getById(int id) async {
    final response = await apiClient.get('$endpoint/$id');
    final data = decodeOrThrow(response);
    return fromJson(data);
  }

  Future<PagedResult<T>> search(Map<String, dynamic> params) => getPage(params: params);
}

/// Full CRUD surface (without delegated-user provisioning).
///
/// Extends [ReadOnlyProvider] with insert/update/remove/uploadImage.
abstract class CrudProvider<T> extends ReadOnlyProvider<T> {
  CrudProvider({
    required super.apiClient,
    required super.endpoint,
  });

  Future<T> uploadImage(
    int id,
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    final response = await apiClient.postMultipart(
      '$endpoint/$id/image',
      bytes: bytes,
      fileName: fileName,
      contentType: contentType,
    );

    final data = decodeOrThrow(response);
    final updated = fromJson(data);
    notifyListeners();
    return updated;
  }

  Future<T?> insert(Map<String, dynamic> request) async {
    final response = await apiClient.post(endpoint, body: request);
    throwIfError(response);

    // The single documented empty-body special case: the backend answers
    // some creates with 204 / an empty body, meaning "created, nothing to
    // return". Callers depend on null here.
    if (response.statusCode == 204 || response.body.isEmpty) {
      notifyListeners();
      return null;
    }

    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }

  Future<T> update(int id, Map<String, dynamic> request) async {
    final response = await apiClient.put('$endpoint/$id', body: request);
    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }

  Future<void> remove(int id) async {
    final response = await apiClient.delete('$endpoint/$id');
    throwIfError(response);
    notifyListeners();
  }
}

/// Wide provider surface kept for backwards compatibility.
///
/// Extends [CrudProvider] with delegated-user provisioning.
/// New read-only providers should extend [ReadOnlyProvider]; pure CRUD
/// providers without delegated creation should extend [CrudProvider].
abstract class BaseProvider<T> extends CrudProvider<T> {
  BaseProvider({
    required super.apiClient,
    required super.endpoint,
  });

  Future<int> createDelegatedUser(DelegatedUserCreateRequest request) async {
    final data = decodeOrThrow(await apiClient.post(endpoint, body: request.toJson()));
    return data['userId'] as int? ?? 0;
  }
}
