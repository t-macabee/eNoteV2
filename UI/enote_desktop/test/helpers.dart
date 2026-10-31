import 'dart:convert';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/pdf_report_button.dart';
import 'package:enote_test_support/enote_test_support.dart' as core;
import 'package:enote_test_support/enote_test_support.dart' hide fakeJwt;
export 'package:enote_test_support/enote_test_support.dart' hide fakeJwt;

String fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
  bool isManager = false,
}) => core.fakeJwt(
      subject: subject, username: username, role: role, isManager: isManager);

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

Future<void> pumpPastDebounce(WidgetTester tester) => tester.pump(
      PagedFetchController.defaultSearchDebounce +
          const Duration(milliseconds: 100),
    );

Future<void> pumpReportScreen(WidgetTester tester, Widget app) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  await tester.pumpWidget(app);
  await tester.pumpAndSettle();
}

Future<void> tapPdfDownload(WidgetTester tester) async {
  expect(find.byType(PdfReportButton), findsOneWidget);
  await tester.tap(
    find.descendant(
      of: find.byType(PdfReportButton),
      matching: find.byIcon(Icons.download),
    ),
  );
  await tester.pumpAndSettle();
}

class DelegatedUserHttpClient extends http.BaseClient {
  final List<String> postUrls = [];
  final List<String> postBodies = [];
  final List<String> putUrls = [];
  final List<String> putBodies = [];
  final List<String> getUrls = [];
  bool simulate400 = false;
  List<Map<String, dynamic>>? shopEmployees;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final url = request.url.toString();
    if (request.method == 'GET') {
      getUrls.add(url);
    }

    if (request.method == 'POST') {
      if (request is http.Request) {
        postUrls.add(url);
        postBodies.add(request.body);
      }
      if (simulate400) {
        return http.StreamedResponse(
          Stream.value(utf8.encode(jsonEncode({'message': 'Neispravan zahtjev.'}))),
          400,
          headers: {'content-type': 'application/json'},
        );
      }
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode({'userId': 42}))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    if (request.method == 'PUT') {
      if (request is http.Request) {
        putUrls.add(url);
        putBodies.add(request.body);
      }
      return http.StreamedResponse(
        Stream.value(utf8.encode('')),
        204,
        headers: {'content-type': 'application/json'},
      );
    }

    if (request.method == 'GET' && url.contains('instructor/students')) {
      final json = jsonEncode({
        'items': [
          {
            'id': 1,
            'appUserId': 10,
            'firstName': 'Edin',
            'lastName': 'Dzeko',
            'username': 'edzeko',
          }
        ],
        'page': 1,
        'pageSize': 10,
        'totalCount': 1,
      });
      return http.StreamedResponse(
        Stream.value(utf8.encode(json)),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    if (request.method == 'GET' && url.contains('shop/employees')) {
      final items = shopEmployees ?? [
        {
          'id': 1,
          'appUserId': 20,
          'musicStoreId': 1,
          'firstName': 'Miralem',
          'lastName': 'Pjanic',
          'username': 'mpjanic',
          'isManager': true,
          'isActive': true,
        }
      ];
      final json = jsonEncode({
        'items': items,
        'page': 1,
        'pageSize': 10,
        'totalCount': items.length,
      });
      return http.StreamedResponse(
        Stream.value(utf8.encode(json)),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

typedef RecordedRequest = ({String method, String url, Map<String, dynamic>? body});

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
