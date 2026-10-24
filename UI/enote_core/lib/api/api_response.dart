import 'dart:convert';

import 'package:http/http.dart' as http;

import 'api_error_mapper.dart';
import 'api_exception.dart';

void throwIfError(http.Response response) {
  if (response.statusCode >= 400) {
    throw ApiException(
      ApiErrorMapper.mapError(response.statusCode, response.body),
      code: ApiError.fromResponseBody(response.body).code,
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
