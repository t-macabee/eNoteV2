import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/testing/testing.dart' as core;
import 'package:enote_core/testing/testing.dart' hide fakeJwt;
export 'package:enote_core/testing/testing.dart' hide fakeJwt;

/// Desktop default identity; see [core.fakeJwt] for the claim layout.
String fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
  bool isManager = false,
}) => core.fakeJwt(
      subject: subject, username: username, role: role, isManager: isManager);

/// Pumps long enough for [PagedFetchController]'s search debounce to elapse,
/// derived from the actual constant (+100 ms buffer for fake-clock safety) so
/// widget-test pumps and the controller cannot drift.
Future<void> pumpPastDebounce(WidgetTester tester) => tester.pump(
      PagedFetchController.defaultSearchDebounce +
          const Duration(milliseconds: 100),
    );

typedef RecordedRequest = ({String method, String url, Map<String, dynamic>? body});

/// Convenience views over [ScriptedClient.requests] for assertions that used to
/// read bespoke recording fields on per-test doubles.
extension ScriptedClientRecords on ScriptedClient {
  List<RecordedRequest> get recorded => [
        for (final r in requests)
          (
            method: r.method,
            url: r.url.toString(),
            body: r.body.isEmpty
                ? null
                : jsonDecode(r.body) as Map<String, dynamic>,
          ),
      ];

  List<String> get requestedUrls => [for (final r in requests) r.url.toString()];

  List<String> get getUrls =>
      [for (final r in requests) if (r.method == 'GET') r.url.toString()];

  List<String> get postUrls =>
      [for (final r in requests) if (r.method == 'POST') r.url.toString()];

  List<String> get putUrls =>
      [for (final r in requests) if (r.method == 'PUT') r.url.toString()];

  List<String> get deletedUrls =>
      [for (final r in requests) if (r.method == 'DELETE') r.url.toString()];


  List<String> get postedBodies =>
      [for (final r in requests) if (r.method == 'POST') r.body];

  List<String> get putBodies =>
      [for (final r in requests) if (r.method == 'PUT') r.body];


}
