import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_attendance_screen.dart';
import 'package:enote_desktop/features/instructor/lecture/lecture_provider.dart';
import 'package:enote_desktop/widgets/status_chip.dart';

import '../../../helpers.dart';

Map<String, Object?> _attendanceJson({
  required int studentId,
  required String attendanceStatus,
}) => {
  'id': studentId,
  'studentId': studentId,
  'studentName': 'Student $studentId',
  'attendanceStatus': attendanceStatus,
};

Future<ScriptedClient> _pumpAttendance(
  WidgetTester tester,
  List<Map<String, Object?>> rows,
) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });

  final client = ScriptedClient((request) {
    if (request.method == 'PUT') {
      return jsonResponse(rows.first, 200);
    }
    return jsonResponse({
      'items': rows,
      'page': 1,
      'pageSize': 20,
      'totalCount': rows.length,
    }, 200);
  });
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
        home: Scaffold(body: LectureAttendanceView(lectureId: 5)),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return client;
}

Finder _actionButton(String label) => find.widgetWithText(TextButton, label);

void main() {
  testWidgets('pending row shows Prisutan and Odsutan', (tester) async {
    await _pumpAttendance(tester, [
      _attendanceJson(studentId: 1, attendanceStatus: 'Pending'),
    ]);

    expect(_actionButton('Prisutan'), findsOneWidget);
    expect(_actionButton('Odsutan'), findsOneWidget);
  });

  testWidgets('present row shows only Odsutan', (tester) async {
    await _pumpAttendance(tester, [
      _attendanceJson(studentId: 1, attendanceStatus: 'Present'),
    ]);

    expect(_actionButton('Prisutan'), findsNothing);
    expect(_actionButton('Odsutan'), findsOneWidget);
  });

  testWidgets('status cell is a StatusChip with the attendance tone', (
    tester,
  ) async {
    await _pumpAttendance(tester, [
      _attendanceJson(studentId: 1, attendanceStatus: 'Absent'),
    ]);

    final chip = tester.widget<StatusChip>(
      find.widgetWithText(StatusChip, 'Odsutan'),
    );
    expect(chip.tone, StatusTone.danger);
  });

  testWidgets('tap Prisutan PUTs present without a dialog', (tester) async {
    final client = await _pumpAttendance(tester, [
      _attendanceJson(studentId: 1, attendanceStatus: 'Pending'),
    ]);

    await tester.tap(_actionButton('Prisutan'));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(client.putUrls.length, 1);
    expect(
      jsonDecode(client.putBodies.single),
      equals({'studentId': 1, 'attendanceStatus': 'Present'}),
    );
    expect(find.text('Prisustvo ažurirano: Prisutan'), findsOneWidget);
  });
}
