import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_detail_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_enrollments_view.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';
import 'package:enote_desktop/features/instructor/enrollment/instructor_enrollment_provider.dart';
import 'package:enote_desktop/features/instructor/ranking/ranking_provider.dart';

import '../../../helpers.dart';

ScriptedClient _client({String? failMessage}) => ScriptedClient((request) {
  if (failMessage != null &&
      (request.method == 'PUT' || request.method == 'DELETE')) {
    return jsonResponse({'message': failMessage}, 400);
  }
  if (request.method == 'PUT' &&
      request.url.path.endsWith('instructor/courses/1')) {
    final body = jsonDecode(request.body) as Map<String, dynamic>;
    return jsonResponse({
      'id': 1,
      'instructorId': 10,
      'name': body['name'] ?? 'Gitara',
      'price': 100.0,
      'enrolledCount': 3,
      'isPublished': body['isPublished'] ?? true,
      'startDate': body['startDate'],
      'endDate': body['endDate'],
    }, 200);
  }

  return jsonResponse(const {
    'items': [],
    'page': 1,
    'pageSize': 20,
    'totalCount': 0,
  }, 200);
});

ApiClient _apiClient(ScriptedClient httpClient) => ApiClient(
  baseUrl: 'http://localhost:5059/api/v1/',
  authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
  httpClient: httpClient,
);

Widget _app(ApiClient apiClient, Widget home) => MultiProvider(
  providers: [
    Provider<ApiClient>.value(value: apiClient),
    ChangeNotifierProvider<CourseProvider>.value(
      value: CourseProvider(apiClient: apiClient),
    ),
    ChangeNotifierProvider<InstructorEnrollmentProvider>.value(
      value: InstructorEnrollmentProvider(apiClient: apiClient),
    ),
    Provider<RankingProvider>.value(
      value: RankingProvider(apiClient: apiClient),
    ),
  ],
  child: home,
);

void _resizeSurface(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });
}

void main() {
  CourseDto course({
    bool published = true,
    String? description,
    int enrolledCount = 3,
  }) => CourseDto(
    id: 1,
    instructorId: 10,
    name: 'Gitara',
    description: description,
    isPublished: published,
    price: 100,
    enrolledCount: enrolledCount,
    startDate: DateTime(2026, 9, 1),
    endDate: DateTime(2026, 12, 1),
  );

  Future<void> pumpScreen(
    WidgetTester tester,
    ScriptedClient httpClient,
    CourseDto dto,
  ) async {
    _resizeSurface(tester);

    await tester.pumpWidget(
      _app(
        _apiClient(httpClient),
        MaterialApp(
          key: ObjectKey(dto),
          home: CourseDetailScreen(course: dto),
        ),
      ),
    );
    await tester.pump();
  }

  Future<void> pumpOpener(
    WidgetTester tester,
    ScriptedClient httpClient,
    CourseDto dto,
    void Function(bool?) onResult,
  ) async {
    _resizeSurface(tester);

    await tester.pumpWidget(
      _app(
        _apiClient(httpClient),
        MaterialApp(
          home: Builder(
            builder: (context) => Scaffold(
              body: Center(
                child: ElevatedButton(
                  onPressed: () async {
                    final result = await CourseDetailScreen.open(context, dto);
                    onResult(result);
                  },
                  child: const Text('Otvori'),
                ),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Otvori'));
    await tester.pumpAndSettle();
  }

  testWidgets(
    'unpublishing shows confirm dialog and does not PUT until confirmed',
    (tester) async {
      final httpClient = _client();
      await pumpScreen(tester, httpClient, course(published: true));

      await tester.tap(find.byType(Switch));
      await tester.pumpAndSettle();

      expect(find.byType(AlertDialog), findsOneWidget);
      expect(find.text('Potvrdite povlačenje kursa'), findsOneWidget);
      expect(find.text('Potvrdi'), findsOneWidget);
      expect(
        httpClient.putUrls,
        isEmpty,
        reason: 'no request may leave before the dialog is confirmed',
      );

      await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
      await tester.pumpAndSettle();

      expect(httpClient.putUrls.length, 1);
      final body =
          jsonDecode(httpClient.putBodies.single) as Map<String, dynamic>;
      expect(body['isPublished'], isFalse);
    },
  );

  testWidgets('cancelling the unpublish confirm dialog does not PUT', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpScreen(tester, httpClient, course(published: true));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(TextButton, 'Otkaži'));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(httpClient.putUrls, isEmpty);
  });

  testWidgets('publishing an unpublished course PUTs without confirmation', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpScreen(tester, httpClient, course(published: false));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(httpClient.putUrls.length, 1);
    final body =
        jsonDecode(httpClient.putBodies.single) as Map<String, dynamic>;
    expect(body['isPublished'], isTrue);
  });

  testWidgets('a failed publish shows the banner and re-enables the switch', (
    tester,
  ) async {
    final httpClient = _client(failMessage: 'Izmjena kursa nije uspjela.');
    await pumpScreen(tester, httpClient, course(published: false));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(httpClient.putUrls.length, 1);
    expect(find.text('Izmjena kursa nije uspjela.'), findsOneWidget);
    final switchWidget = tester.widget<Switch>(find.byType(Switch));
    expect(switchWidget.onChanged, isNotNull);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'a blocked delete shows the server message and keeps the screen open',
    (tester) async {
      const blockedMessage =
          'Kurs ima studente s plaćenim pristupom do 31.07.2026. Uklonite objavu kursa da ga sakrijete iz ponude; brisanje je moguće nakon isteka.';
      final httpClient = _client(failMessage: blockedMessage);
      await pumpScreen(tester, httpClient, course());

      await tester.tap(find.widgetWithText(TextButton, 'Obriši'));
      await tester.pumpAndSettle();
      expect(find.text('Potvrdite brisanje'), findsOneWidget);

      await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
      await tester.pumpAndSettle();

      expect(httpClient.deletedUrls.length, 1);
      expect(find.text(blockedMessage), findsOneWidget);
      expect(find.byType(CourseDetailScreen), findsOneWidget);
      final deleteButton = tester.widget<TextButton>(
        find.widgetWithText(TextButton, 'Obriši'),
      );
      expect(deleteButton.onPressed, isNotNull);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('shows five tabs in order', (tester) async {
    final httpClient = _client();
    await pumpScreen(tester, httpClient, course());

    const labels = ['Pregled', 'Predavanja', 'Objave', 'Upisi', 'Rangiranje'];
    final lefts = [
      for (final label in labels) tester.getTopLeft(find.text(label)).dx,
    ];

    expect(find.text('Pregled'), findsOneWidget);
    expect(find.text('Predavanja'), findsOneWidget);
    expect(find.text('Objave'), findsOneWidget);
    expect(find.text('Upisi'), findsOneWidget);
    expect(find.text('Rangiranje'), findsOneWidget);

    for (var i = 0; i < lefts.length - 1; i++) {
      expect(
        lefts[i],
        lessThan(lefts[i + 1]),
        reason: '${labels[i]} must be left of ${labels[i + 1]}',
      );
    }
  });

  testWidgets('Uredi, Objavljen switch and Obriši are in the app bar', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpScreen(tester, httpClient, course());

    final appBar = find.byType(AppBar);
    expect(
      find.descendant(
        of: appBar,
        matching: find.widgetWithText(TextButton, 'Uredi'),
      ),
      findsOneWidget,
    );
    expect(
      find.descendant(of: appBar, matching: find.text('Objavljen')),
      findsOneWidget,
    );
    expect(
      find.descendant(of: appBar, matching: find.byType(Switch)),
      findsOneWidget,
    );
    expect(
      find.descendant(
        of: appBar,
        matching: find.widgetWithText(TextButton, 'Obriši'),
      ),
      findsOneWidget,
    );
  });

  testWidgets('overview shows description, price, dates and enrolled count', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpScreen(
      tester,
      httpClient,
      course(description: 'Napredni kurs gitare', enrolledCount: 7),
    );

    expect(find.text('Napredni kurs gitare'), findsOneWidget);
    expect(find.text('100.00'), findsOneWidget);
    expect(find.text('01.09.2026.'), findsOneWidget);
    expect(find.text('01.12.2026.'), findsOneWidget);
    expect(find.text('7'), findsOneWidget);

    await pumpScreen(tester, httpClient, course(description: ''));
    expect(find.text('-'), findsOneWidget);
  });

  testWidgets('overview is not vertically centered', (tester) async {
    final httpClient = _client();
    await pumpScreen(tester, httpClient, course());

    final top = tester.getTopLeft(find.text('Podaci o kursu')).dy;
    expect(top, lessThan(200));
  });

  testWidgets('tapping Upisi shows the enrollments list filtered by Pending', (
    tester,
  ) async {
    final httpClient = _client();
    await pumpScreen(tester, httpClient, course());

    await tester.tap(find.text('Upisi'));
    await tester.pumpAndSettle();

    final enrollmentRequests = httpClient.requests.where(
      (r) => r.method == 'GET' && r.url.path.endsWith('/enrollments'),
    );
    expect(enrollmentRequests, isNotEmpty);
    expect(
      enrollmentRequests.first.url.queryParameters['enrollmentStatus'],
      'Pending',
    );
  });

  testWidgets('closing returns false when nothing changed', (tester) async {
    final httpClient = _client();
    bool? result;
    await pumpOpener(tester, httpClient, course(), (value) => result = value);

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(result, isFalse);
  });

  testWidgets('closing returns true after a publish change', (tester) async {
    final httpClient = _client();
    bool? result;
    await pumpOpener(
      tester,
      httpClient,
      course(published: false),
      (value) => result = value,
    );

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();
    expect(httpClient.putUrls.length, 1);

    await tester.tap(find.byTooltip('Zatvori'));
    await tester.pumpAndSettle();

    expect(result, isTrue);
  });

  ScriptedClient enrollmentCountClient(int pendingCount) =>
      ScriptedClient((request) {
        if (request.url.path.endsWith('/enrollments')) {
          return jsonResponse({
            'items': [],
            'page': 1,
            'pageSize': 1,
            'totalCount': pendingCount,
          }, 200);
        }
        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }, 200);
      });

  testWidgets('Upisi tab shows the pending count', (tester) async {
    await pumpScreen(tester, enrollmentCountClient(2), course());
    await tester.pumpAndSettle();

    expect(find.text('Upisi (2)'), findsOneWidget);
  });

  testWidgets('Upisi tab shows no count when nothing is pending', (
    tester,
  ) async {
    await pumpScreen(tester, enrollmentCountClient(0), course());
    await tester.pumpAndSettle();

    expect(find.text('Upisi'), findsOneWidget);
    expect(find.textContaining('Upisi ('), findsNothing);
  });

  testWidgets('CourseDetailScreen opens on the tab from initialTab', (
    tester,
  ) async {
    _resizeSurface(tester);
    await tester.pumpWidget(
      _app(
        _apiClient(_client()),
        MaterialApp(
          home: CourseDetailScreen(
            course: course(),
            initialTab: CourseDetailTab.enrollments,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(CourseEnrollmentsView), findsOneWidget);
    final controller = DefaultTabController.of(
      tester.element(find.byType(TabBar)),
    );
    expect(controller.index, CourseDetailTab.enrollments.index);
  });
}
