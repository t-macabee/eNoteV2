import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_detail_dialog.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_list_screen.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/widgets/entity_list_config.dart';
import 'package:enote_desktop/widgets/status_chip.dart';

import '../../../helpers.dart';

Map<String, Object?> _lectureJson({
  required int id,
  required String lectureStatus,
  bool isCancelled = false,
}) => {
  'id': id,
  'name': 'Predavanje $id',
  'location': 'Sala 1',
  'lectureType': 'Theoretical',
  'lectureStatus': lectureStatus,
  'isCancelled': isCancelled,
  'cancellationReason': isCancelled ? 'Bolest predavača' : null,
  'cancelledAt': isCancelled
      ? DateTime(2026, 10, 18, 9).toIso8601String()
      : null,
  'lectureTime': DateTime(2026, 10, 18, 10).toIso8601String(),
  'duration': 60,
  'capacity': 30,
  'attendeeCount': 0,
};

Future<void> _pumpList(
  WidgetTester tester,
  List<Map<String, Object?>> rows,
) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });

  final client = ScriptedClient(
    (request) => jsonResponse({
      'items': rows,
      'page': 1,
      'pageSize': 20,
      'totalCount': rows.length,
    }, 200),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<LectureProvider>.value(
          value: LectureProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: LectureListScreen(
            courseId: 3,
            courseName: 'Gitara',
            presentation: EntityListPresentation.embedded,
            listStyle: EntityListStyle.table,
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('status cell is a StatusChip with the lecture tone', (
    tester,
  ) async {
    await _pumpList(tester, [_lectureJson(id: 1, lectureStatus: 'Scheduled')]);

    final chip = tester.widget<StatusChip>(
      find.widgetWithText(StatusChip, 'Zakazano'),
    );
    expect(chip.tone, StatusTone.info);
  });

  testWidgets('cancelled lecture shows the danger chip', (tester) async {
    await _pumpList(tester, [
      _lectureJson(id: 2, lectureStatus: 'Scheduled', isCancelled: true),
    ]);

    final chip = tester.widget<StatusChip>(
      find.widgetWithText(StatusChip, 'Otkazano'),
    );
    expect(chip.tone, StatusTone.danger);
  });

  testWidgets('detail dialog shows Otkazano for a cancelled lecture', (
    tester,
  ) async {
    final lecture = LectureDto.fromJson({
      ..._lectureJson(id: 3, lectureStatus: 'Scheduled', isCancelled: true),
      'cancelledAt': null,
    });
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: LectureDetailDialog(lecture: lecture)),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Otkazano'), findsOneWidget);
    expect(find.text('Zakazano'), findsNothing);
  });
}
