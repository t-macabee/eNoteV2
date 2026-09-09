import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

String base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

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
          DateTime.now()
              .add(const Duration(days: 1))
              .millisecondsSinceEpoch ~/
          1000,
    }),
  );
  return '$header.$payload.signature';
}

class RecordingHttpClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final int statusCode;
  final Map<String, dynamic> body;

  RecordingHttpClient({this.statusCode = 200, this.body = const {}});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request as http.Request);
    final bytes = utf8.encode(jsonEncode(body));
    return http.StreamedResponse(
      Stream.value(bytes),
      statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

Future<void> pumpPastDebounce(WidgetTester tester) => tester.pump(
  PagedFetchController.defaultSearchDebounce + const Duration(milliseconds: 100),
);
