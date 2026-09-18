/// Test-only helpers shared by the enote_core, enote_mobile and enote_desktop
/// test suites. Not exported from `enote_core.dart`; import directly.
library;

import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

String base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

/// Unsigned JWT accepted by `AuthState` in tests. Each app's `test/helpers.dart`
/// may wrap this with its own default identity.
String fakeJwt({
  String subject = '1',
  String username = 'student',
  String role = 'Student',
  bool isManager = false,
}) {
  final header = base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = base64UrlSegment(
    jsonEncode({
      'sub': subject,
      'unique_name': username,
      'role': role,
      if (isManager) 'is_manager': true,
      'exp':
          DateTime.now().add(const Duration(days: 1)).millisecondsSinceEpoch ~/
          1000,
    }),
  );
  return '$header.$payload.signature';
}

/// JSON response; a [String] body is sent verbatim, anything else is encoded.
http.Response jsonResponse(
  Object? body,
  int status, {
  Map<String, String>? headers,
}) {
  final text = body is String ? body : jsonEncode(body);
  return http.Response.bytes(
    utf8.encode(text),
    status,
    headers: headers ?? const {'content-type': 'application/json'},
  );
}

/// Maps a request to a response and records every request. Defaults to 404
/// when no handler is supplied. Keeps the `send()` → [http.StreamedResponse]
/// boilerplate in one place.
class ScriptedClient extends http.BaseClient {
  ScriptedClient([this.handler]);

  final FutureOr<http.Response> Function(http.Request request)? handler;
  final List<http.Request> requests = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final recorded = request is http.Request
        ? request
        : http.Request(request.method, request.url);
    requests.add(recorded);
    final response = handler == null
        ? jsonResponse('', 404)
        : await handler!(recorded);
    final headers = <String, String>{...response.headers};
    final contentType = headers['content-type'];
    if (contentType == null) {
      headers['content-type'] = 'application/json; charset=utf-8';
    } else if (!contentType.contains('charset')) {
      headers['content-type'] = '$contentType; charset=utf-8';
    }
    return http.StreamedResponse(
      Stream.value(response.bodyBytes),
      response.statusCode,
      headers: headers,
    );
  }
}

/// Fixed-response convenience over [ScriptedClient].
class RecordingHttpClient extends ScriptedClient {
  RecordingHttpClient({int statusCode = 200, Map<String, dynamic> body = const {}})
      : super((_) => jsonResponse(body, statusCode));
}
