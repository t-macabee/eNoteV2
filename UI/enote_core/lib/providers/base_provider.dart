import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../api/api_client.dart';
import '../api/api_response.dart';
import '../models/identity/auth_models.dart';
import '../paging/paged_result.dart';

abstract class ReadOnlyProvider<T> with ChangeNotifier {
  final ApiClient apiClient;
  final String endpoint;

  bool _disposed = false;

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
  }) => decodePage(response, fromJsonT, params: params);

  Future<T> getById(int id) async {
    final response = await apiClient.get('$endpoint/$id');
    final data = decodeOrThrow(response);
    return fromJson(data);
  }

  Future<PagedResult<T>> search(Map<String, dynamic> params) => getPage(params: params);

  @override
  void notifyListeners() {
    if (!_disposed) super.notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}

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

abstract class DelegatedUserProvider<T> extends CrudProvider<T> {
  DelegatedUserProvider({
    required super.apiClient,
    required super.endpoint,
  });

  Future<void> createDelegatedUser(RegisterRequest request) async {
    final response = await apiClient.post(endpoint, body: request.toJson());
    throwIfError(response);
  }
}
