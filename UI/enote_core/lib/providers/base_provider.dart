import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../api/api_client.dart';
import '../api/api_error_mapper.dart';
import '../api/api_exception.dart';
import '../paging/paged_result.dart';

abstract class BaseProvider<T> with ChangeNotifier {
  final ApiClient apiClient;
  final String endpoint;

  BaseProvider({
    required this.apiClient,
    required this.endpoint,
  });

  T fromJson(Map<String, dynamic> json);

  Future<PagedResult<T>> getPage({Map<String, dynamic>? params}) async {
    final response = await apiClient.get(endpoint, queryParams: params);

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final items = (data['items'] as List<dynamic>? ?? []);
    final page = data['page'] as int? ?? params?['page'] as int? ?? 1;
    final pageSize = data['pageSize'] as int? ?? params?['pageSize'] as int? ?? 20;
    final totalCount = data['totalCount'] as int?;

    final result = PagedResult<T>(
      items: items.map((e) => fromJson(Map<String, dynamic>.from(e))).toList(),
      page: page,
      pageSize: pageSize,
      totalCount: totalCount,
    );

    notifyListeners();
    return result;
  }

  Future<T> getById(int id) async {
    final response = await apiClient.get('$endpoint/$id');

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return fromJson(data);
  }

  Future<T?> insert(Map<String, dynamic> request) async {
    final response = await apiClient.post(endpoint, body: request);

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    if (response.statusCode == 204 || response.body.isEmpty) {
      notifyListeners();
      return null;
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    notifyListeners();
    return fromJson(data);
  }

  Future<T> update(int id, Map<String, dynamic> request) async {
    final response = await apiClient.put('$endpoint/$id', body: request);

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    notifyListeners();
    return fromJson(data);
  }

  Future<void> remove(int id) async {
    final response = await apiClient.delete('$endpoint/$id');

    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    notifyListeners();
  }

  Future<PagedResult<T>> search(Map<String, dynamic> params) => getPage(params: params);
}
