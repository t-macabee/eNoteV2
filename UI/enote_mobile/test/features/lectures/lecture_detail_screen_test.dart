import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/lectures/lecture_detail_screen.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';
import 'package:enote_mobile/features/lectures/rsvp_sheet.dart';

import '../../helpers.dart';

const _lectureJson = {
  'id': 11,
  'name': 'Akordi I',
  'location': 'Sala 2',
  'lectureType': 'Theoretical',
  'lectureStatus': 'Scheduled',
  'isCancelled': false,
  'lectureTime': '2026-09-14T18:00:00',
  'duration': 60,
  'capacity': 20,
  'attendeeCount': 5,
  'myAttendanceStatus': 'Pending',
};

class _LectureStubClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(_lectureJson))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _GatedLectureProvider extends LectureProvider {
  Completer<RsvpResponse>? rsvpGate;

  _GatedLectureProvider({required super.apiClient});

  @override
  Future<RsvpResponse> rsvp(int id, RsvpRequest request) {
    rsvpGate ??= Completer<RsvpResponse>();
    return rsvpGate!.future;
  }
}

void main() {
  testWidgets(
      'completing the RSVP after the detail screen is gone throws nothing',
      (tester) async {
    final client = _LectureStubClient();
    final authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    final apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
    final lectures = _GatedLectureProvider(apiClient: apiClient);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<LectureProvider>.value(value: lectures),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => Center(
                child: FilledButton(
                  onPressed: () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => const LectureDetailScreen(lectureId: 11),
                    ),
                  ),
                  child: const Text('Open lecture'),
                ),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open lecture'));
    await tester.pumpAndSettle();
    expect(find.text('Akordi I'), findsOneWidget);

    await tester.tap(
      find.descendant(
        of: find.byType(LectureDetailScreen),
        matching: find.widgetWithText(FilledButton, 'Dolazim'),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.byType(RsvpSheet), findsOneWidget);

    await tester.tap(
      find.descendant(
        of: find.byType(RsvpSheet),
        matching: find.widgetWithText(FilledButton, 'Dolazim'),
      ),
    );
    // No settle here: the sheet shows an indeterminate spinner while the
    // RSVP future is pending, which never settles.
    await tester.pump();
    await tester.pump();

    final sheetElement = tester.element(find.byType(RsvpSheet));
    final navigator = Navigator.of(sheetElement);
    final sheetRoute = ModalRoute.of(sheetElement)!;
    navigator.removeRouteBelow(sheetRoute);
    await tester.pump();
    await tester.pump();
    expect(find.text('Akordi I'), findsNothing);

    lectures.rsvpGate!.complete(
      RsvpResponse(lectureId: 11, studentId: 1, confirmed: true),
    );
    await tester.pump();
    await tester.pump();

    expect(find.byType(RsvpSheet), findsOneWidget);
  });
}
