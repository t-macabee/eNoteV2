import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/assignment_submission/submission_list_screen.dart';
import 'package:enote_desktop/features/instructor/assignment_submission/submission_provider.dart';

import 'package:enote_desktop/widgets/file_save.dart';

import '../../../helpers.dart';

Map<String, dynamic> _submissionJson({
  required int id,
  required String studentName,
  int? grade,
  String? filePath,
}) => {
  'id': id,
  'assignmentId': 2,
  'studentId': id,
  'studentName': studentName,
  'grade': grade,
  'filePath': filePath,
  'submittedAt': '2026-10-01T10:00:00',
};

final _rows = [
  _submissionJson(
    id: 1,
    studentName: 'Jana Janić',
    filePath: '/api/v1/uploads/assignments/abc.pdf',
  ),
  _submissionJson(id: 2, studentName: 'Ivan Ivić'),
  _submissionJson(id: 3, studentName: 'Ana Anić', grade: 90),
];

Future<ScriptedClient> _pumpList(
  WidgetTester tester, {
  FileSaver saveFile = saveFileWithDialog,
  int downloadStatus = 200,
}) async {
  tester.view.physicalSize = const Size(1400, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = ScriptedClient((request) {
    if (request.url.path.contains('/uploads/assignments/')) {
      return http.Response.bytes(
        downloadStatus == 200 ? [37, 80, 68, 70] : [],
        downloadStatus,
      );
    }
    final isCount = request.url.queryParameters['pageSize'] == '1';
    final isGraded = request.url.queryParameters['isGraded'];
    final matching = _rows
        .where(
          (row) =>
              isGraded == null ||
              (row['grade'] != null) == (isGraded == 'true'),
        )
        .toList();
    return jsonResponse({
      'items': isCount ? matching.take(1).toList() : matching,
      'page': 1,
      'pageSize': isCount ? 1 : 20,
      'totalCount': matching.length,
    }, 200);
  });

  final authState = AuthState(
    baseUrl: 'http://localhost/',
    httpClient: client,
    tokenReader: () => fakeJwt(username: 'test', role: 'Instructor'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost/',
    authState: authState,
    httpClient: client,
  );
  final provider = SubmissionProvider(
    apiClient: apiClient,
    lectureId: 1,
    assignmentId: 2,
  );

  await tester.pumpWidget(
    MaterialApp(
      home: MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<SubmissionProvider>.value(value: provider),
        ],
        child: SubmissionListScreen(
          lectureId: 1,
          assignmentId: 2,
          assignmentTitle: 'Test zadatak',
          saveFile: saveFile,
        ),
      ),
    ),
  );

  await tester.pumpAndSettle();
  return client;
}

void main() {
  testWidgets('a row with a file offers the download, a row without does not', (
    tester,
  ) async {
    await _pumpList(tester);

    expect(find.byTooltip('Preuzmi datoteku'), findsOneWidget);
    expect(find.text('Nema datoteke'), findsOneWidget);
  });

  testWidgets('the download saves the bytes under a readable name', (
    tester,
  ) async {
    Uint8List? savedBytes;
    String? savedName;
    final client = await _pumpList(
      tester,
      saveFile: (bytes, name) async {
        savedBytes = bytes;
        savedName = name;
        return '/tmp/$name';
      },
    );

    await tester.tap(find.byTooltip('Preuzmi datoteku'));
    await tester.pumpAndSettle();

    expect(client.getUrls.last, 'http://localhost/uploads/assignments/abc.pdf');
    expect(savedBytes, [37, 80, 68, 70]);
    expect(savedName, 'Jana Janić - Test zadatak.pdf');
    expect(find.textContaining('Datoteka sačuvana'), findsOneWidget);
  });

  testWidgets('a failed download shows the message and saves nothing', (
    tester,
  ) async {
    var saved = false;
    await _pumpList(
      tester,
      downloadStatus: 403,
      saveFile: (bytes, name) async {
        saved = true;
        return name;
      },
    );

    await tester.tap(find.byTooltip('Preuzmi datoteku'));
    await tester.pumpAndSettle();

    expect(saved, isFalse);
    expect(find.text('Nemate pristup ovom resursu.'), findsOneWidget);
  });

  testWidgets('SubmissionListScreen shows the student name search', (
    tester,
  ) async {
    await _pumpList(tester);

    expect(find.byType(TextField), findsOneWidget);
    expect(find.text('Pretraži po imenu studenta...'), findsOneWidget);
  });

  testWidgets('opens on Neocijenjeno with isGraded false', (tester) async {
    final client = await _pumpList(tester);

    final firstList = Uri.parse(client.getUrls.first);
    expect(firstList.queryParameters['isGraded'], 'false');
  });

  testWidgets('Neocijenjeno tab shows the count', (tester) async {
    await _pumpList(tester);

    expect(find.text('Neocijenjeno (2)'), findsOneWidget);
    expect(find.text('Ocijenjeno'), findsOneWidget);
    expect(find.text('Sve'), findsOneWidget);
  });

  testWidgets('ungraded row shows the warning chip', (tester) async {
    await _pumpList(tester);
    await tester.tap(find.text('Sve'));
    await tester.pumpAndSettle();

    final chips = tester.widgetList<StatusChip>(
      find.widgetWithText(StatusChip, 'Neocijenjeno'),
    );
    expect(chips, hasLength(2));
    expect(chips.every((chip) => chip.tone == StatusTone.warning), isTrue);
  });

  testWidgets('Sve tab sends no isGraded', (tester) async {
    final client = await _pumpList(tester);

    await tester.tap(find.text('Sve'));
    await tester.pumpAndSettle();

    final lastList = Uri.parse(client.getUrls.last);
    expect(lastList.queryParameters.containsKey('isGraded'), isFalse);
  });

  testWidgets('SubmissionListScreen sends the student name and graded filter', (
    tester,
  ) async {
    final client = await _pumpList(tester);

    await tester.enterText(find.byType(TextField), 'Jana');
    await pumpPastDebounce(tester);
    await tester.pumpAndSettle();

    final query = Uri.parse(client.getUrls.last).queryParameters;
    expect(query['studentName'], 'Jana');
    expect(query['isGraded'], 'false');
  });
}
