import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/detail_row.dart';

class CourseDetailRows extends StatelessWidget {
  final CourseDto course;

  const CourseDetailRows({super.key, required this.course});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        DetailRow(
          icon: Icons.description_outlined,
          label: 'Opis',
          value: (course.description != null && course.description!.isNotEmpty)
              ? course.description!
              : '-',
        ),
        DetailRow(
          icon: Icons.payments_outlined,
          label: 'Mjesečna cijena',
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
    );
  }
}
