import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import 'lecture_labels.dart';

class LectureTile extends StatelessWidget {
  final LectureDto lecture;
  final VoidCallback onTap;

  const LectureTile({super.key, required this.lecture, required this.onTap});

  static IconData _iconFor(LectureType type) => switch (type) {
    LectureType.theoretical => Icons.school_outlined,
    LectureType.practical => Icons.music_note_outlined,
    LectureType.combined => Icons.event_note_outlined,
  };

  @override
  Widget build(BuildContext context) {
    final cancellationReason = lecture.cancellationReason;
    final chips = lectureStateChips(lecture);

    return ListTile(
      onTap: onTap,
      leading: Icon(_iconFor(lecture.lectureType)),
      title: Text(lecture.name),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text('${formatDateTime(lecture.lectureTime)} · ${lecture.location}'),
          if (cancellationReason != null)
            Text(
              'Razlog: $cancellationReason',
              style: Theme.of(context).textTheme.bodySmall
                  ?.copyWith(color: AppTheme.error),
            ),
          if (chips.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            Wrap(
              spacing: AppSpacing.xs,
              runSpacing: AppSpacing.xs,
              children: chips,
            ),
          ],
        ],
      ),
      trailing: StatusChip(
        label: lectureStatusLabel(lecture.lectureStatus),
        tone: lectureStatusTone(lecture.lectureStatus),
      ),
    );
  }
}
