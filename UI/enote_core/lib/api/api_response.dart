import 'dart:convert';

import 'package:http/http.dart' as http;

import 'api_error_mapper.dart';
import 'api_exception.dart';

/// Throws [ApiException] with a user-facing Bosnian message if [response]
/// carries an error status. Returns normally otherwise.
void throwIfError(http.Response response) {
  if (response.statusCode >= 400) {
    throw ApiException(
      ApiErrorMapper.mapError(response.statusCode, response.body),
    );
  }
}

/// [throwIfError], then decodes the body as a JSON object.
///
/// An empty, malformed, or non-object body throws an [ApiException] with a
/// distinct Bosnian message (not the offline fallback from [userMessage]),
/// so a parse failure is never misreported as a connectivity problem.
Map<String, dynamic> decodeOrThrow(http.Response response) =>
    _decodeOrThrow<Map<String, dynamic>>(response);

/// [decodeOrThrow] for bare-list endpoints.
List<dynamic> decodeListOrThrow(http.Response response) =>
    _decodeOrThrow<List<dynamic>>(response);

T _decodeOrThrow<T>(http.Response response) {
  throwIfError(response);
  try {
    final decoded = jsonDecode(response.body);
    if (decoded is T) return decoded;
  } on FormatException {
    // fall through
  }
  throw ApiException('Neispravan odgovor servera.');
}
