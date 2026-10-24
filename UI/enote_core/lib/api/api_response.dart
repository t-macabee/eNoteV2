import 'dart:convert';

import 'package:http/http.dart' as http;

import '../paging/paged_result.dart';
import 'api_error_mapper.dart';
import 'api_exception.dart';

void throwIfError(http.Response response) {
  if (response.statusCode >= 400) {
    final error = ApiError.fromResponseBody(response.body);
    throw ApiException(
      ApiErrorMapper.mapError(response.statusCode, response.body),
      code: error.code,
      errors: error.errors,
    );
  }
}

Map<String, dynamic> decodeOrThrow(http.Response response) =>
    _decodeOrThrow<Map<String, dynamic>>(response);

List<dynamic> decodeListOrThrow(http.Response response) =>
    _decodeOrThrow<List<dynamic>>(response);

List<dynamic> decodeListOrThrowBody(String body) =>
    _decodeBodyOrThrow<List<dynamic>>(body);

List<T> decodeList<T>(
  http.Response response,
  T Function(Map<String, dynamic>) fromJson,
) => [
  for (final e in decodeListOrThrow(response))
    fromJson(Map<String, dynamic>.from(e as Map)),
];

PagedResult<T> decodePage<T>(
  http.Response response,
  T Function(Map<String, dynamic>) fromJson, {
  Map<String, dynamic>? params,
}) {
  final data = decodeOrThrow(response);
  try {
    final items = (data['items'] as List<dynamic>? ?? []);
    final page = data['page'] as int? ?? params?['page'] as int? ?? 1;
    final pageSize = data['pageSize'] as int? ?? params?['pageSize'] as int? ?? 20;
    final totalCount = data['totalCount'] as int?;
    return PagedResult<T>(
      items: items.map((e) => fromJson(Map<String, dynamic>.from(e))).toList(),
      page: page,
      pageSize: pageSize,
      totalCount: totalCount,
    );
  } catch (e) {
    if (e is ApiException) rethrow;
    throw ApiException('Neispravan odgovor servera.');
  }
}

T _decodeOrThrow<T>(http.Response response) {
  throwIfError(response);
  return _decodeBodyOrThrow<T>(response.body);
}

T _decodeBodyOrThrow<T>(String body) {
  try {
    final decoded = jsonDecode(body);
    if (decoded is T) return decoded;
  } on FormatException catch (_) {
  }
  throw ApiException('Neispravan odgovor servera.');
}
