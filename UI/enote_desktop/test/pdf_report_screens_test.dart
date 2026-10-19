import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_list_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/features/instructor/course/course_ranking_dialog.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_workspace_dialog.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_list_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';
import 'package:enote_desktop/widgets/pdf_report_button.dart';

import 'helpers.dart';

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

  Future<void> pumpApp(WidgetTester tester, Widget app) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(app);
    await tester.pumpAndSettle();
  }

  Future<void> tapDownload(WidgetTester tester) async {
    expect(find.byType(PdfReportButton), findsOneWidget);
    await tester.tap(
      find.descendant(
        of: find.byType(PdfReportButton),
        matching: find.byIcon(Icons.download),
      ),
    );
    await tester.pumpAndSettle();
  }

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

    await pumpApp(
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
    await tapDownload(tester);

    expect(
      client.getUrls.where((u) => u.endsWith('/ranking/report')),
      hasLength(1),
    );
    expect(
      picker.savedFileName,
      matches(RegExp(r'^course-5-ranking-\d{8}\.pdf$')),
    );
  });

  testWidgets('lecture workspace reports attendance under a dated name',
      (tester) async {
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
            }
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

    await pumpApp(
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

    await tapDownload(tester);

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

  testWidgets('rental list reports the store rentals under a dated name',
      (tester) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.endsWith('/shop/rentals/report')) {
        return jsonResponse('%PDF-1.7', 200);
      }
      return jsonResponse(_emptyPage(), 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await pumpApp(
      tester,
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<InstrumentProvider>.value(
            value: InstrumentProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<RentalProvider>.value(
            value: RentalProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: Scaffold(body: RentalListScreen()),
        ),
      ),
    );
    await pumpPastDebounce(tester);

    await tapDownload(tester);

    expect(
      client.getUrls.where((u) => u.endsWith('/shop/rentals/report')),
      hasLength(1),
    );
    expect(
      picker.savedFileName,
      matches(RegExp(r'^store-rentals-\d{8}\.pdf$')),
    );
  });

  testWidgets('music store list reports every store under a dated name',
      (tester) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.endsWith('/admin/music-stores/report')) {
        return jsonResponse('%PDF-1.7', 200);
      }
      return jsonResponse(_emptyPage(), 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await pumpApp(
      tester,
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<MusicStoreProvider>.value(
            value: MusicStoreProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<CityProvider>.value(
            value: CityProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: Scaffold(body: MusicStoreListScreen()),
        ),
      ),
    );
    await pumpPastDebounce(tester);

    await tapDownload(tester);

    expect(
      client.getUrls.where((u) => u.endsWith('/admin/music-stores/report')),
      hasLength(1),
    );
    expect(
      picker.savedFileName,
      matches(RegExp(r'^music-stores-report-\d{8}\.pdf$')),
    );
  });
}
