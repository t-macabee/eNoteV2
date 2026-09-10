import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/assignments/assignment_detail_screen.dart';
import 'package:enote_mobile/features/assignments/assignment_provider.dart';
import 'package:enote_mobile/features/assignments/file_picker_field.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

Map<String, dynamic> _assignmentJson({required String dueAt}) => {
  'id': 5,
  'lectureId': 11,
  'title': 'Akordi — vježba 1',
  'description': 'Snimite sebe kako svirate akorde.',
  'dueAt': dueAt,
};

Map<String, dynamic> _submissionJson({int? grade}) => {
  'id': 3,
  'assignmentId': 5,
  'studentId': 7,
  'studentName': 'Student Enote',
  'filePath': '/api/v1/uploads/assignments/abc.pdf',
  'submittedAt': '2026-09-09T18:20:00',
  'grade': grade,
};

class _AssignmentDetailStubClient extends http.BaseClient {
  final List<String> calls = [];
  final String dueAt;
  final Map<String, dynamic>? submission;
  bool submitted = false;

  _AssignmentDetailStubClient({required this.dueAt, this.submission});

  int count(String marker) =>
      calls.where((c) => c == marker || c.startsWith('$marker?')).length;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    calls.add('${request.method} $path?${request.url.query}');
    Object body;
    int status = 200;
    switch ('${request.method} $path') {
      case 'GET /api/v1/student/assignments/5':
        body = _assignmentJson(dueAt: dueAt);
      case 'GET /api/v1/student/assignments/5/submission':
        if (submitted && submission == null) {
          body = _submissionJson();
        } else if (submission != null) {
          body = submission!;
        } else {
          status = 404;
          body = {'message': 'Predaja zadatka nije pronađena.'};
        }
      case 'POST /api/v1/student/assignments/5/submit':
        submitted = true;
        body = _submissionJson();
      default:
        body = {'message': 'OK'};
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      status,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _Harness {
  late final _AssignmentDetailStubClient client;
  late final ApiClient apiClient;

  _Harness({required String dueAt, Map<String, dynamic>? submission}) {
    client = _AssignmentDetailStubClient(dueAt: dueAt, submission: submission);
    final authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      tokenWriter: (_) {},
      httpClient: client,
    );
    apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
  }

  Widget app({Future<PickedAssignmentFile?> Function()? pickFiles}) {
    return MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<AssignmentProvider>(
          create: (_) => AssignmentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        home: AssignmentDetailScreen(
          assignmentId: 5,
          courseId: 2,
          pickFiles: pickFiles,
        ),
      ),
    );
  }

  Widget appWithoutCourse() {
    return MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<AssignmentProvider>(
          create: (_) => AssignmentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        home: const AssignmentDetailScreen(assignmentId: 5),
      ),
    );
  }
}

Future<PickedAssignmentFile?> _pdfOf(int bytes, [String name = 'vjezba1.pdf']) {
  return Future.value(
    PickedAssignmentFile(
      bytes: Uint8List.fromList(List<int>.filled(bytes, 0)),
      fileName: name,
      contentType: assignmentContentType(name),
    ),
  );
}

void main() {
  testWidgets('state A renders the picker with the submit disabled', (
    tester,
  ) async {
    final harness = _Harness(dueAt: '2026-09-20T23:59:00');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Akordi — vježba 1'), findsOneWidget);
    expect(find.text('Odaberi datoteku'), findsOneWidget);
    expect(
      find.text('PDF, JPG ili PNG · do 5 MB'),
      findsOneWidget,
    );
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Predaj zadatak'),
    );
    expect(button.onPressed, isNull);
  });

  testWidgets('state A-prime shows the past-due banner and no picker', (
    tester,
  ) async {
    final harness = _Harness(dueAt: '2026-09-01T00:00:00');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Rok istekao'), findsOneWidget);
    expect(
      find.text('Rok za predaju zadatka je istekao.'),
      findsOneWidget,
    );
    expect(find.text('Odaberi datoteku'), findsNothing);
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Predaj zadatak'),
    );
    expect(button.onPressed, isNull);
  });

  testWidgets('state B shows the submitted copy and hides the picker', (
    tester,
  ) async {
    final harness = _Harness(
      dueAt: '2026-09-20T23:59:00',
      submission: _submissionJson(),
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Predano 09.09.2026. 18:20'), findsOneWidget);
    expect(find.textContaining('Još nije ocijenjeno'), findsOneWidget);
    expect(
      find.text('Zadatak je već predan. Ponovna predaja nije moguća.'),
      findsOneWidget,
    );
    expect(find.text('Odaberi datoteku'), findsNothing);
    expect(
      find.widgetWithText(FilledButton, 'Predaj zadatak'),
      findsNothing,
    );
  });

  testWidgets('state C shows the numeral and the ranking shortcut', (
    tester,
  ) async {
    final harness = _Harness(
      dueAt: '2026-09-20T23:59:00',
      submission: _submissionJson(grade: 87),
    );
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('87'), findsOneWidget);
    expect(find.text('Ocjena (od 100)'), findsOneWidget);
    expect(find.text('Rang lista kursa'), findsOneWidget);
  });

  testWidgets('state C hides the ranking shortcut without a courseId', (
    tester,
  ) async {
    final harness = _Harness(
      dueAt: '2026-09-20T23:59:00',
      submission: _submissionJson(grade: 87),
    );
    await tester.pumpWidget(harness.appWithoutCourse());
    await tester.pumpAndSettle();

    expect(find.text('87'), findsOneWidget);
    expect(find.text('Rang lista kursa'), findsNothing);
  });

  testWidgets('an oversize file is rejected below the picker', (tester) async {
    final harness = _Harness(dueAt: '2026-09-20T23:59:00');
    await tester.pumpWidget(
      harness.app(pickFiles: () => _pdfOf(6 * 1024 * 1024, 'velika.pdf')),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Odaberi datoteku'));
    await tester.pumpAndSettle();

    expect(find.text('Datoteka je veća od 5 MB.'), findsOneWidget);
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Predaj zadatak'),
    );
    expect(button.onPressed, isNull);
    expect(
      harness.client.count('POST /api/v1/student/assignments/5/submit'),
      0,
    );
  });

  testWidgets('a wrong-type file is rejected below the picker', (tester) async {
    final harness = _Harness(dueAt: '2026-09-20T23:59:00');
    await tester.pumpWidget(
      harness.app(pickFiles: () => _pdfOf(1024, 'vjezba1.docx')),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Odaberi datoteku'));
    await tester.pumpAndSettle();

    expect(
      find.text('Dozvoljeni formati: PDF, JPG, PNG.'),
      findsOneWidget,
    );
    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Predaj zadatak'),
    );
    expect(button.onPressed, isNull);
  });

  testWidgets('a valid file enables submit and confirms before sending', (
    tester,
  ) async {
    final harness = _Harness(dueAt: '2026-09-20T23:59:00');
    await tester.pumpWidget(
      harness.app(pickFiles: () => _pdfOf(1024)),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Odaberi datoteku'));
    await tester.pumpAndSettle();
    expect(find.textContaining('vjezba1.pdf'), findsWidgets);

    final button = tester.widget<FilledButton>(
      find.widgetWithText(FilledButton, 'Predaj zadatak'),
    );
    expect(button.onPressed, isNotNull);

    await tester.tap(find.widgetWithText(FilledButton, 'Predaj zadatak'));
    await tester.pumpAndSettle();
    expect(find.text('Predaja zadatka'), findsOneWidget);
    expect(
      find.text('Predati "vjezba1.pdf"? Predaju nije moguće izmijeniti.'),
      findsOneWidget,
    );

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      harness.client.count('POST /api/v1/student/assignments/5/submit'),
      1,
    );
    expect(find.text('Zadatak je predan.'), findsOneWidget);
    expect(
      find.text('Zadatak je već predan. Ponovna predaja nije moguća.'),
      findsOneWidget,
    );
  });

  testWidgets('the lecture row navigates without rendering the id', (
    tester,
  ) async {
    final harness = _Harness(dueAt: '2026-09-20T23:59:00');
    await tester.pumpWidget(harness.app());
    await tester.pumpAndSettle();

    expect(find.text('Predavanje'), findsOneWidget);
    expect(find.textContaining('11'), findsNothing);
  });
}
