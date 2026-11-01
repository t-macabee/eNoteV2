import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_workspace_dialog.dart';

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

  testWidgets('lecture workspace reports attendance under a dated name', (
    tester,
  ) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.endsWith('/attendance/report')) {
        return jsonResponse('%PDF-1.7', 200);
      }
      if (path.endsWith('/attendance')) {
        return jsonResponse(_emptyPage(), 200);
      }
      if (path.endsWith('/lectures')) {
        return jsonResponse({
          'items': [
            {
              'id': 9,
              'name': 'Prvo predavanje',
              'location': 'Sala 1',
              'lectureType': 'Theoretical',
              'lectureStatus': 'Scheduled',
              'isCancelled': false,
              'lectureTime': DateTime(2026, 10, 18, 10).toIso8601String(),
              'duration': 60,
              'capacity': 30,
              'attendeeCount': 0,
            },
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        }, 200);
      }
      return jsonResponse(_emptyPage(), 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await pumpReportScreen(
      tester,
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<LectureProvider>.value(
            value: LectureProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: LectureWorkspaceDialog(
              apiClient: apiClient,
              courseId: 3,
              courseName: 'Gitara',
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.byTooltip('Prisustvo'));
    await tester.pumpAndSettle();

    await tapPdfDownload(tester);

    expect(
      client.getUrls.where(
        (u) => u.endsWith('/instructor/lectures/9/attendance/report'),
      ),
      hasLength(1),
    );
    expect(
      picker.savedFileName,
      matches(RegExp(r'^lecture-9-attendance-\d{8}\.pdf$')),
    );
  });
}
