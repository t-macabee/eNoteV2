import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/lectures/lecture_detail_screen.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';

import '../../helpers.dart';

Map<String, dynamic> _lectureJson({required String lectureTime}) => {
  'id': 11,
  'name': 'Akordi I',
  'location': 'Sala 2',
  'lectureType': 'Theoretical',
  'lectureStatus': 'Scheduled',
  'isCancelled': false,
  'lectureTime': lectureTime,
  'duration': 60,
  'capacity': 20,
  'attendeeCount': 5,
  'myAttendanceStatus': 'Pending',
};

final _futureLectureTime = DateTime.now()
    .add(const Duration(days: 365))
    .toIso8601String();
final _pastLectureTime = DateTime.now()
    .subtract(const Duration(hours: 1))
    .toIso8601String();

class _GatedLectureProvider extends LectureProvider {
  Completer<RsvpResponse>? rsvpGate;

  _GatedLectureProvider({required super.apiClient});

  @override
  Future<RsvpResponse> rsvp(int id, RsvpRequest request) {
    rsvpGate ??= Completer<RsvpResponse>();
    return rsvpGate!.future;
  }
}

Future<void> _pumpLectureDetail(
  WidgetTester tester,
  ScriptedClient client,
) async {
  setPhoneSurface(tester);
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
  await tester.pumpWidget(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
      ],
      child: const MaterialApp(home: LectureDetailScreen(lectureId: 11)),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'completing the RSVP after the detail screen is gone throws nothing',
    (tester) async {
      final client = ScriptedClient(
        (_) => jsonResponse(
          jsonEncode(_lectureJson(lectureTime: _futureLectureTime)),
          200,
        ),
      );
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
                        builder: (_) =>
                            const LectureDetailScreen(lectureId: 11),
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
      await tester.pump();

      final detailElement = tester.element(find.byType(LectureDetailScreen));
      Navigator.of(detailElement).pop();
      await tester.pumpAndSettle();
      expect(find.text('Akordi I'), findsNothing);

      lectures.rsvpGate!.complete(
        RsvpResponse(lectureId: 11, studentId: 1, confirmed: true),
      );
      await tester.pump();
      await tester.pump();

      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('Dolazim posts the RSVP directly without a bottom sheet', (
    tester,
  ) async {
    final requests = <String>[];
    final client = ScriptedClient((request) {
      if (request.method == 'POST') {
        requests.add('${request.method} ${request.url.path}');
        return jsonResponse(
          jsonEncode({'lectureId': 11, 'studentId': 7, 'confirmed': true}),
          200,
        );
      }
      return jsonResponse(
        jsonEncode(_lectureJson(lectureTime: _futureLectureTime)),
        200,
      );
    });
    await _pumpLectureDetail(tester, client);

    await tester.tap(find.widgetWithText(FilledButton, 'Dolazim'));
    await tester.pumpAndSettle();

    expect(find.byType(BottomSheet), findsNothing);
    expect(requests, ['POST /api/v1/student/lectures/11/rsvp']);
    expect(find.text('Prisustvo je potvrđeno.'), findsOneWidget);
  });

  testWidgets('a confirmed lecture shows only Ne dolazim', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          ..._lectureJson(lectureTime: _futureLectureTime),
          'myAttendanceStatus': 'Present',
        }),
        200,
      ),
    );
    await _pumpLectureDetail(tester, client);

    expect(find.widgetWithText(FilledButton, 'Dolazim'), findsNothing);
    expect(find.widgetWithText(OutlinedButton, 'Ne dolazim'), findsOneWidget);
  });

  testWidgets('a declined lecture shows only Dolazim', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          ..._lectureJson(lectureTime: _futureLectureTime),
          'myAttendanceStatus': 'Absent',
        }),
        200,
      ),
    );
    await _pumpLectureDetail(tester, client);

    expect(find.widgetWithText(FilledButton, 'Dolazim'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Ne dolazim'), findsNothing);
  });

  testWidgets('an RSVP error shows a banner and keeps both buttons', (
    tester,
  ) async {
    final client = ScriptedClient((request) {
      if (request.method == 'POST') {
        return jsonResponse(
          jsonEncode({'message': 'Predavanje je popunjeno.'}),
          409,
        );
      }
      return jsonResponse(
        jsonEncode(_lectureJson(lectureTime: _futureLectureTime)),
        200,
      );
    });
    await _pumpLectureDetail(tester, client);

    await tester.tap(find.widgetWithText(FilledButton, 'Dolazim'));
    await tester.pumpAndSettle();

    expect(find.text('Predavanje je popunjeno.'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Dolazim'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Ne dolazim'), findsOneWidget);
  });

  testWidgets('canceled lecture hides the RSVP buttons', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          ..._lectureJson(lectureTime: _futureLectureTime),
          'lectureStatus': 'Cancelled',
          'isCancelled': true,
          'cancellationReason': 'Instrument je pokvaren',
        }),
        200,
      ),
    );
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
    final lectures = LectureProvider(apiClient: apiClient);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<LectureProvider>.value(value: lectures),
        ],
        child: const MaterialApp(home: LectureDetailScreen(lectureId: 11)),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Otkazano'), findsOneWidget);
    expect(find.text('Razlog otkazivanja'), findsOneWidget);
    expect(find.text('Instrument je pokvaren'), findsOneWidget);
    expect(find.text('Predavanje je otkazano.'), findsNothing);
    expect(find.widgetWithText(FilledButton, 'Dolazim'), findsNothing);
    expect(find.widgetWithText(OutlinedButton, 'Ne dolazim'), findsNothing);
    expect(find.widgetWithText(OutlinedButton, 'Bilješke'), findsOneWidget);
  });

  testWidgets('held lecture hides the RSVP buttons', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          ..._lectureJson(lectureTime: _pastLectureTime),
          'lectureStatus': 'Held',
        }),
        200,
      ),
    );
    await _pumpLectureDetail(tester, client);

    expect(find.text('Održano'), findsOneWidget);
    expect(find.text('Predavanje je održano.'), findsNothing);
    expect(
      find.text('Prijave su zatvorene jer je predavanje počelo.'),
      findsNothing,
    );
    expect(find.widgetWithText(FilledButton, 'Dolazim'), findsNothing);
    expect(find.widgetWithText(OutlinedButton, 'Ne dolazim'), findsNothing);
    expect(find.widgetWithText(OutlinedButton, 'Bilješke'), findsOneWidget);
  });

  testWidgets(
    'started lecture hides the RSVP buttons and shows the closed banner',
    (tester) async {
      final client = ScriptedClient(
        (_) => jsonResponse(
          jsonEncode(_lectureJson(lectureTime: _pastLectureTime)),
          200,
        ),
      );
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
      final lectures = LectureProvider(apiClient: apiClient);

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthState>.value(value: authState),
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<LectureProvider>.value(value: lectures),
          ],
          child: const MaterialApp(home: LectureDetailScreen(lectureId: 11)),
        ),
      );

      await tester.pumpAndSettle();

      expect(
        find.text('Prijave su zatvorene jer je predavanje počelo.'),
        findsOneWidget,
      );
      expect(find.widgetWithText(FilledButton, 'Dolazim'), findsNothing);
      expect(find.widgetWithText(OutlinedButton, 'Ne dolazim'), findsNothing);
      expect(find.widgetWithText(OutlinedButton, 'Bilješke'), findsOneWidget);
    },
  );

  testWidgets(
    'full lecture shows disabled RSVP buttons under the full banner',
    (tester) async {
      final client = ScriptedClient(
        (_) => jsonResponse(
          jsonEncode({
            ..._lectureJson(lectureTime: _futureLectureTime),
            'attendeeCount': 20,
          }),
          200,
        ),
      );
      await _pumpLectureDetail(tester, client);

      expect(find.text('Predavanje je popunjeno.'), findsOneWidget);
      final arrive = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Dolazim'),
      );
      final decline = tester.widget<OutlinedButton>(
        find.widgetWithText(OutlinedButton, 'Ne dolazim'),
      );
      expect(arrive.onPressed, isNull);
      expect(decline.onPressed, isNull);
      expect(find.widgetWithText(OutlinedButton, 'Bilješke'), findsOneWidget);
    },
  );

  testWidgets('held lecture header has no RSVP chip', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          ..._lectureJson(lectureTime: _pastLectureTime),
          'lectureStatus': 'Held',
        }),
        200,
      ),
    );
    await _pumpLectureDetail(tester, client);

    expect(find.text('Održano'), findsOneWidget);
    expect(find.text('Niste odgovorili'), findsNothing);
  });

  testWidgets('an individual lecture is marked as such', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(
        jsonEncode({
          ..._lectureJson(lectureTime: _futureLectureTime),
          'studentId': 7,
          'studentName': 'Amar Hodžić',
        }),
        200,
      ),
    );
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
    final lectures = LectureProvider(apiClient: apiClient);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<LectureProvider>.value(value: lectures),
        ],
        child: const MaterialApp(home: LectureDetailScreen(lectureId: 11)),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Individualni čas'), findsOneWidget);
  });
}
