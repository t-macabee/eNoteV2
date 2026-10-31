import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_ranking_dialog.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_provider.dart';

import '../../../helpers.dart';

Map<String, Object?> _emptyPage() => const {
      'items': [],
      'page': 1,
      'pageSize': 100,
      'totalCount': 0,
    };

void main() {
  late FakeSaveFilePicker picker;

  setUp(() {
    picker = FakeSaveFilePicker();
    FilePicker.platform = picker;
  });

  testWidgets('ranking dialog reports the course ranking under a dated name',
      (tester) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.endsWith('/ranking/report')) {
        return jsonResponse('%PDF-1.7', 200);
      }
      if (path.endsWith('/ranking')) {
        return jsonResponse([
          {
            'rank': 1,
            'studentId': 7,
            'studentName': 'Student Enote',
            'averageGrade': 9.5,
            'gradedSubmissions': 3,
          },
        ], 200);
      }
      return jsonResponse(_emptyPage(), 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );
    final rankingProvider = RankingProvider(apiClient: apiClient);

    await pumpReportScreen(
      tester,
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          Provider<RankingProvider>.value(value: rankingProvider),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: CourseRankingDialog(
              courseId: 5,
              reportEndpoint: rankingProvider.reportEndpoint(5),
            ),
          ),
        ),
      ),
    );

    expect(find.text('Izvještaj – ocijenjeni studenti'), findsOneWidget);
    await tapPdfDownload(tester);

    expect(
      client.getUrls.where((u) => u.endsWith('/ranking/report')),
      hasLength(1),
    );
    expect(
      picker.savedFileName,
      matches(RegExp(r'^course-5-ranking-\d{8}\.pdf$')),
    );
  });
}
