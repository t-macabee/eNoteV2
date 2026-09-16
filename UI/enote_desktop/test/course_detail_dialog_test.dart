import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_detail_dialog.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';

class _RecordingHttpClient extends http.BaseClient {
  final List<http.BaseRequest> requests = [];

  Iterable<http.BaseRequest> get puts => requests.where((r) => r.method == 'PUT');

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request);

    if (request.method == 'PUT' &&
        request.url.path.endsWith('instructor/courses/1')) {
      final body =
          jsonDecode((request as http.Request).body) as Map<String, dynamic>;
      final json = {
        'id': 1,
        'instructorId': 10,
        'name': body['name'] ?? 'Gitara',
        'price': 100.0,
        'enrolledCount': 3,
        'isPublished': body['isPublished'] ?? true,
        'startDate': body['startDate'],
        'endDate': body['endDate'],
      };
      return http.StreamedResponse(
        Stream.value(utf8.encode(jsonEncode(json))),
        200,
        headers: {'content-type': 'application/json'},
      );
    }

    return http.StreamedResponse(
      Stream.value(utf8
          .encode(jsonEncode({'items': [], 'page': 1, 'pageSize': 20, 'totalCount': 0}))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

void main() {
  CourseDto course({bool published = true}) => CourseDto(
        id: 1,
        instructorId: 10,
        name: 'Gitara',
        isPublished: published,
        price: 100,
        enrolledCount: 3,
        startDate: DateTime(2026, 9, 1),
        endDate: DateTime(2026, 12, 1),
      );

  Future<void> pumpDialog(
    WidgetTester tester,
    _RecordingHttpClient httpClient,
    CourseDto dto,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: httpClient,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<CourseProvider>.value(
            value: CourseProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(body: CourseDetailDialog(course: dto)),
        ),
      ),
    );
    await tester.pump();
  }

  testWidgets(
      'unpublishing shows confirm dialog and does not PUT until confirmed',
      (tester) async {
    final httpClient = _RecordingHttpClient();
    await pumpDialog(tester, httpClient, course(published: true));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsOneWidget);
    expect(find.text('Potvrdite povlačenje kursa'), findsOneWidget);
    expect(find.text('Potvrdi'), findsOneWidget);
    expect(httpClient.puts, isEmpty,
        reason: 'no request may leave before the dialog is confirmed');

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.puts.length, 1);
    final body =
        jsonDecode((httpClient.puts.single as http.Request).body)
            as Map<String, dynamic>;
    expect(body['isPublished'], isFalse);
  });

  testWidgets('cancelling the unpublish confirm dialog does not PUT',
      (tester) async {
    final httpClient = _RecordingHttpClient();
    await pumpDialog(tester, httpClient, course(published: true));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(TextButton, 'Otkaži'));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(httpClient.puts, isEmpty);
  });

  testWidgets('publishing an unpublished course PUTs without confirmation',
      (tester) async {
    final httpClient = _RecordingHttpClient();
    await pumpDialog(tester, httpClient, course(published: false));

    await tester.tap(find.byType(Switch));
    await tester.pumpAndSettle();

    expect(find.byType(AlertDialog), findsNothing);
    expect(httpClient.puts.length, 1);
    final body =
        jsonDecode((httpClient.puts.single as http.Request).body)
            as Map<String, dynamic>;
    expect(body['isPublished'], isTrue);
  });
}
