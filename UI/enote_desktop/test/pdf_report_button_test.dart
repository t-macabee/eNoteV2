import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/pdf_report_button.dart';

import 'helpers.dart';

const _fileName = 'course-7-ranking-20261018.pdf';
const _pdfBody = '%PDF-1.7 izvjestaj';

void main() {
  late Directory tempDir;
  late FakeSaveFilePicker picker;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('enote_pdf_report');
    picker = FakeSaveFilePicker(path: '${tempDir.path}/$_fileName');
    FilePicker.platform = picker;
  });

  tearDown(() async {
    if (tempDir.existsSync()) {
      await tempDir.delete(recursive: true);
    }
  });

  Future<void> pumpButton(
    WidgetTester tester,
    ScriptedClient client, {
    PdfSaver? savePdf,
  }) async {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: PdfReportButton(
              label: 'Izvještaj',
              fileName: _fileName,
              endpoint: 'instructor/courses/7/ranking/report',
              savePdf: savePdf,
            ),
          ),
        ),
      ),
    );
  }

  testWidgets('download saves the fetched report under the given file name',
      (tester) async {
    final client = ScriptedClient(
      (request) => jsonResponse(_pdfBody, 200, headers: const {
        'content-type': 'application/pdf',
      }),
    );
    String? savedName;
    Uint8List? savedBytes;
    const savedPath = r'C:\izvjestaji\course-7-ranking-20261018.pdf';
    await pumpButton(
      tester,
      client,
      savePdf: (bytes, fileName) async {
        savedName = fileName;
        savedBytes = bytes;
        return savedPath;
      },
    );

    await tester.tap(find.byIcon(Icons.download));
    await tester.pumpAndSettle();

    expect(
      client.getUrls.single,
      endsWith('/api/v1/instructor/courses/7/ranking/report'),
    );
    expect(savedName, _fileName);
    expect(savedBytes, utf8.encode(_pdfBody));
    expect(find.text('Izvještaj sačuvan: $savedPath'), findsOneWidget);
  });

  testWidgets('cancelling the save dialog writes no file and shows no message',
      (tester) async {
    final client = ScriptedClient((_) => jsonResponse(_pdfBody, 200));
    picker.path = null;
    await pumpButton(tester, client);

    await tester.tap(find.byIcon(Icons.download));
    await tester.pumpAndSettle();

    expect(client.getUrls, hasLength(1));
    expect(picker.savedFileName, _fileName);
    expect(tempDir.listSync(), isEmpty);
    expect(find.textContaining('Izvještaj sačuvan'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('a 403 fetch shows the Bosnian error and keeps the icons usable',
      (tester) async {
    final client = ScriptedClient((_) => jsonResponse(const {}, 403));
    await pumpButton(tester, client);

    await tester.tap(find.byIcon(Icons.download));
    await tester.pumpAndSettle();

    expect(find.text('Nemate pristup ovom resursu.'), findsOneWidget);
    expect(picker.savedFileName, isNull);
    expect(
      tester
          .widget<IconButton>(find.widgetWithIcon(IconButton, Icons.download))
          .onPressed,
      isNotNull,
    );
    expect(
      tester
          .widget<IconButton>(find.widgetWithIcon(IconButton, Icons.print))
          .onPressed,
      isNotNull,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('reportFileName stamps the name with yyyyMMdd', (tester) async {
    final now = DateTime.now();
    final stamp = '${now.year}'
        '${now.month.toString().padLeft(2, '0')}'
        '${now.day.toString().padLeft(2, '0')}';

    expect(reportFileName('store-rentals'), 'store-rentals-$stamp.pdf');
    expect(
      RegExp(r'^lecture-12-attendance-\d{8}\.pdf$')
          .hasMatch(reportFileName('lecture-12-attendance')),
      isTrue,
    );
  });
}
