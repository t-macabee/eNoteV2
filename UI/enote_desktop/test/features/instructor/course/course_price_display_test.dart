import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/instructor/course/course_catalog_detail_dialog.dart';
import 'package:enote_desktop/features/instructor/course/course_detail_sections.dart';

CourseDto _course() => CourseDto.fromJson(const {
  'id': 2,
  'instructorId': 1,
  'name': 'Napredne tehnike gitare',
  'description': 'Napredne tehnike i improvizacija.',
  'isPublished': true,
  'startDate': '2026-10-25T00:00:00Z',
  'endDate': '2027-11-08T00:00:00Z',
  'price': 800.0,
  'enrolledCount': 8,
  'instructorName': 'Selma Begić',
});

void main() {
  testWidgets('the catalog detail dialog shows the price in KM', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: CourseCatalogDetailDialog(course: _course())),
      ),
    );

    expect(find.text('800.00 KM'), findsOneWidget);
  });

  testWidgets('the course detail rows show the price in KM', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: CourseDetailRows(course: _course())),
      ),
    );

    expect(find.text('800.00 KM'), findsOneWidget);
  });
}
