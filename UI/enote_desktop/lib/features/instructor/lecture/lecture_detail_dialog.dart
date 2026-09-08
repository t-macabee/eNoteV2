import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';
import 'lecture_type_label.dart';

/// Read-only detail dialog for a single lecture.
class LectureDetailDialog extends StatelessWidget {
  final LectureDto lecture;

  const LectureDetailDialog({
    super.key,
    required this.lecture,
  });

  static Future<void> show(BuildContext context, LectureDto lecture) {
    return showDialog<void>(
      context: context,
      builder: (_) => LectureDetailDialog(lecture: lecture),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DialogShell(
      title: lecture.name,
      width: DialogShellWidth.sm,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            DetailRow(
              icon: Icons.category_outlined,
              label: 'Tip',
              value: lectureTypeLabel(lecture.lectureType),
            ),
            DetailRow(
              icon: Icons.event_outlined,
              label: 'Datum',
              value: formatDate(lecture.lectureTime),
            ),
            DetailRow(
              icon: Icons.schedule_outlined,
              label: 'Vrijeme',
              value: formatTime(lecture.lectureTime),
            ),
            DetailRow(
              icon: Icons.timer_outlined,
              label: 'Trajanje',
              value: '${lecture.duration} min',
            ),
            DetailRow(
              icon: Icons.event_seat_outlined,
              label: 'Kapacitet',
              value: lecture.capacity?.toString() ?? '-',
            ),
            DetailRow(
              icon: Icons.groups_outlined,
              label: 'Broj polaznika',
              value: lecture.attendeeCount.toString(),
            ),
            DetailRow(
              icon: Icons.info_outlined,
              label: 'Status',
              value: lectureStatusLabel(lecture),
            ),
          ],
        ),
      ),
    );
  }
}
