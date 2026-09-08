import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';

/// Read-only catalog detail — deliberately separate from [CourseDetailDialog],
/// which exposes Delete/Uredi actions that must not appear on other
/// instructors' courses.
class CourseCatalogDetailDialog extends StatelessWidget {
  final CourseDto course;

  const CourseCatalogDetailDialog({
    super.key,
    required this.course,
  });

  static Future<void> show(BuildContext context, CourseDto course) {
    return showDialog<void>(
      context: context,
      builder: (_) => CourseCatalogDetailDialog(course: course),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      title: course.name,
      width: DialogShellWidth.sm,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            DetailRow(
              icon: Icons.person_outline,
              label: 'Instruktor',
              value: (course.instructorName != null &&
                      course.instructorName!.isNotEmpty)
                  ? course.instructorName!
                  : '-',
            ),
            DetailRow(
              icon: Icons.payments_outlined,
              label: 'Cijena',
              value: course.price.toStringAsFixed(2),
            ),
            DetailRow(
              icon: Icons.event_outlined,
              label: 'Datum početka',
              value: formatDateNullable(course.startDate),
            ),
            DetailRow(
              icon: Icons.event_available_outlined,
              label: 'Datum završetka',
              value: formatDateNullable(course.endDate),
            ),
            DetailRow(
              icon: Icons.groups_outlined,
              label: 'Broj upisanih',
              value: course.enrolledCount.toString(),
            ),
          ],
        ),
      ),
    );
  }
}
