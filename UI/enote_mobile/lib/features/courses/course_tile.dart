import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class CourseTile extends StatelessWidget {
  final CourseDto course;
  final VoidCallback onTap;
  final String? subtitle;

  const CourseTile({
    super.key,
    required this.course,
    required this.onTap,
    this.subtitle,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      onTap: onTap,
      leading: const Icon(Icons.school_outlined),
      title: Text(course.name),
      subtitle: Text(
        subtitle ??
            '${orDash(course.instructorName)} · '
                '${formatDateNullable(course.startDate)}–'
                '${formatDateNullable(course.endDate)}',
      ),
      trailing: switch (course.enrollmentStatus) {
        null || EnrollmentStatus.canceled => null,
        final status => StatusChip(
          label: enrollmentStatusLabel(status),
          tone: enrollmentStatusTone(status),
        ),
      },
    );
  }
}
