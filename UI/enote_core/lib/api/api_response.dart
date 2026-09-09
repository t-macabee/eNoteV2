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
Map<String, dynamic> decodeOrThrow(http.Response response) {
  throwIfError(response);
  if (response.body.trim().isEmpty) {
    throw ApiException('Neispravan odgovor servera.');
  }
  try {
    final decoded = jsonDecode(response.body);
    if (decoded is Map<String, dynamic>) return decoded;
    throw ApiException('Neispravan odgovor servera.');
  } catch (e) {
    if (e is ApiException) rethrow;
    throw ApiException('Neispravan odgovor servera.');
  }
}
