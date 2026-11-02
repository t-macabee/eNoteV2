import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_list_screen.dart';
import 'package:enote_desktop/features/instructor/course/course_provider.dart';

import '../../../helpers.dart';

const _courses = [
  {
    'id': 1,
    'instructorId': 10,
    'name': 'Classical Piano',
    'price': 150.0,
    'isPublished': true,
    'enrolledCount': 10,
    'startDate': '2026-09-01T00:00:00Z',
  },
  {
    'id': 2,
    'instructorId': 10,
    'name': 'Jazz Guitar',
    'price': 120.0,
    'isPublished': false,
    'enrolledCount': 0,
    'startDate': '2026-10-15T00:00:00Z',
  },
];

ScriptedClient _client() => ScriptedClient((request) {
  final uri = request.url;
  final isPublished = uri.queryParameters['isPublished'];

  if (uri.path.endsWith('/instructor/courses')) {
    final items = _courses
        .where(
          (c) => isPublished == null || '${c['isPublished']}' == isPublished,
        )
        .toList();
    return jsonResponse({
      'items': items,
      'page': 1,
      'pageSize': 24,
      'totalCount': items.length,
    }, 200);
  }

  return jsonResponse(const {
    'items': [],
    'page': 1,
    'pageSize': 24,
    'totalCount': 0,
  }, 200);
});

void main() {
  testWidgets(
    'instructor course grid shows Neobjavljen chip for an unpublished course',
    (tester) async {
      tester.view.physicalSize = const Size(1280, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _client();
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
        httpClient: mockClient,
      );

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<CourseProvider>(
              create: (_) => CourseProvider(apiClient: apiClient),
            ),
          ],
          child: const MaterialApp(home: CourseListScreen()),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(StatusChip), findsOneWidget);
      expect(find.text('Neobjavljen'), findsOneWidget);
      expect(find.text('01.09.2026.'), findsOneWidget);
      expect(find.text('15.10.2026.'), findsOneWidget);
    },
  );
}
