import 'dart:convert';

import 'api_exception.dart';

/// Maps any caught error to Bosnian text safe to show a user.
///
/// [ApiException] already carries a message produced by [ApiErrorMapper], so
/// it passes through unchanged. Anything else (socket errors, timeouts, a
/// stray [FormatException], ...) is a lower-level failure the user has no
/// use for — collapse it to one generic sentence instead of leaking Dart's
/// exception text.
String userMessage(Object e) => e is ApiException
    ? e.message
    : 'Nije moguće povezati se sa serverom. Pokušajte ponovo.';

class ApiError {
  final int status;
  final String code;
  final String message;
  final String title;
  final Map<String, List<String>> errors;

  ApiError({
    required this.status,
    required this.code,
    required this.message,
    this.title = '',
    Map<String, List<String>>? errors,
  }) : errors = errors ?? const {};

  factory ApiError.fromJson(Map<String, dynamic> json) {
    final rawErrors = json['errors'];
    Map<String, List<String>> parsedErrors = const {};
    if (rawErrors is Map) {
      parsedErrors = {
        for (final entry in rawErrors.entries)
          entry.key.toString(): (entry.value is List
              ? (entry.value as List).map((e) => e.toString()).toList()
              : [entry.value.toString()]),
      };
    }
    return ApiError(
      status: json['status'] as int? ?? 0,
      code: json['code'] as String? ?? '',
      message: json['message'] as String? ?? '',
      title: json['title'] as String? ?? '',
      errors: parsedErrors,
    );
  }

  factory ApiError.fromResponseBody(String body) {
    final trimmed = body.trim();
    if (trimmed.isEmpty) {
      return ApiError(status: 0, code: '', message: '');
    }
    try {
      final decoded = jsonDecode(trimmed);
      if (decoded is Map<String, dynamic>) {
        return ApiError.fromJson(decoded);
      }
    } catch (_) {}
    return ApiError(status: 0, code: '', message: trimmed);
  }
}

class ApiErrorMapper {
  ApiErrorMapper._();

  static String mapError(int statusCode, String body) {
    final apiError = ApiError.fromResponseBody(body);

    if (apiError.message.isNotEmpty) {
      return apiError.message;
    }

    if (apiError.errors.isNotEmpty) {
      return apiError.errors.values.expand((e) => e).join('\n');
    }

    if (statusCode == 400 && apiError.title.isNotEmpty) {
      return apiError.title;
    }

    return _defaultMessage(statusCode);
  }

  static String _defaultMessage(int statusCode) {
    return switch (statusCode) {
      400 => 'Neispravan zahtjev.',
      401 => 'Vaša sesija je istekla. Prijavite se ponovo.',
      403 => 'Nemate pristup ovom resursu.',
      404 => 'Resurs nije pronađen.',
      409 => 'Sukob podataka.',
      >= 500 => 'Greška na serveru. Pokušajte ponovo kasnije.',
      _ => 'Došlo je do greške. Pokušajte ponovo.',
    };
  }
}
