import 'dart:convert';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_test_support/enote_test_support.dart' as core;
import 'package:enote_test_support/enote_test_support.dart' hide fakeJwt;
export 'package:enote_test_support/enote_test_support.dart' hide fakeJwt;

/// Desktop default identity; see [core.fakeJwt] for the claim layout.
String fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
  bool isManager = false,
}) => core.fakeJwt(
      subject: subject, username: username, role: role, isManager: isManager);

/// Stands in for the platform save dialog in widget tests: records the
/// suggested file name and returns [path]. A null [path] means the user
/// cancelled. Install with `FilePicker.platform = FakeSaveFilePicker(...)`.
class FakeSaveFilePicker extends FilePicker {
  FakeSaveFilePicker({this.path});

  String? path;
  String? savedFileName;

  @override
  Future<String?> saveFile({
    String? dialogTitle,
    String? fileName,
    String? initialDirectory,
    FileType type = FileType.any,
    List<String>? allowedExtensions,
    Uint8List? bytes,
    bool lockParentWindow = false,
  }) async {
    savedFileName = fileName;
    return path;
  }
}

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
