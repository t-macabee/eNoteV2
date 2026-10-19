import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/course/admin_course_list_screen.dart';
import 'package:enote_desktop/features/admin/course/admin_course_provider.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
      final url = request.url.toString();

      if (url.contains('admin/courses')) {
        return jsonResponse(const {
          'items': [
            {
              'id': 1,
              'instructorId': 10,
              'name': 'Guitar 101',
              'price': 100.0,
              'isPublished': true,
              'enrolledCount': 3,
              'imagePath': '/api/v1/uploads/courses/default.webp',
            },
          ],
          'page': 1,
          'pageSize': 24,
          'totalCount': 1,
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
  testWidgets('the admin course grid passes each course imagePath', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: _client(),
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<AdminCourseProvider>.value(
            value: AdminCourseProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: AdminCourseListScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Guitar 101'), findsOneWidget);
    expect(find.byIcon(Icons.class_), findsOneWidget);

    final grid = tester.widget<EntityGridScreen<CourseDto>>(
      find.byType(EntityGridScreen<CourseDto>),
    );
    final course = CourseDto(
      id: 2,
      instructorId: 10,
      name: 'Piano 101',
      isPublished: true,
      price: 0,
      enrolledCount: 0,
      imagePath: '/api/v1/uploads/courses/default.webp',
    );
    expect(grid.config.imageUrlOf!(course), course.imagePath);
  });
}
