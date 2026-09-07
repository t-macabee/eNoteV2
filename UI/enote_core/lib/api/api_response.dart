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
Map<String, dynamic> decodeOrThrow(http.Response response) {
  throwIfError(response);
  if (response.body.trim().isEmpty) {
    throw const FormatException('Response body is empty.');
  }
  return jsonDecode(response.body) as Map<String, dynamic>;
}
