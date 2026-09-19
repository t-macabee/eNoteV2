import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/detail_row.dart';

class CourseDetailActionBar extends StatelessWidget {
  final VoidCallback onLectures;
  final VoidCallback onAnnouncements;
  final VoidCallback onRanking;

  const CourseDetailActionBar({
    super.key,
    required this.onLectures,
    required this.onAnnouncements,
    required this.onRanking,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 0),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          OutlinedButton.icon(
            icon: const Icon(Icons.event_note, size: 18),
            label: const Text('Predavanja'),
            onPressed: onLectures,
          ),
          OutlinedButton.icon(
            icon: const Icon(Icons.campaign, size: 18),
            label: const Text('Objave'),
            onPressed: onAnnouncements,
          ),
          OutlinedButton.icon(
            icon: const Icon(Icons.leaderboard, size: 18),
            label: const Text('Rangiranje'),
            onPressed: onRanking,
          ),
        ],
      ),
    );
  }
}

class CourseDetailRows extends StatelessWidget {
  final CourseDto course;

  const CourseDetailRows({
    super.key,
    required this.course,
  });

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

class CourseDetailFooter extends StatelessWidget {
  final bool isPublished;
  final bool isDeleting;
  final bool isPublishing;
  final VoidCallback onDelete;
  final ValueChanged<bool> onPublishChanged;
  final VoidCallback onEdit;

  const CourseDetailFooter({
    super.key,
    required this.isPublished,
    required this.isDeleting,
    required this.isPublishing,
    required this.onDelete,
    required this.onPublishChanged,
    required this.onEdit,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          OutlinedButton.icon(
            onPressed: isDeleting || isPublishing ? null : onDelete,
            style: OutlinedButton.styleFrom(
              foregroundColor: AppTheme.error,
              side: const BorderSide(color: AppTheme.error),
            ),
            icon: isDeleting
                ? const SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppTheme.error,
                    ),
                  )
                : const Icon(Icons.delete_outline, size: 18),
            label: const Text('Obriši'),
          ),
          const Spacer(),
          const Text(
            'Objavljen',
            style: TextStyle(color: AppTheme.textSecondary),
          ),
          const SizedBox(width: 8),
          if (isPublishing)
            const SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          else
            Switch(
              value: isPublished,
              onChanged: isDeleting ? null : onPublishChanged,
            ),
          const SizedBox(width: 8),
          FilledButton.icon(
            onPressed: isDeleting || isPublishing ? null : onEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Uredi'),
          ),
        ],
      ),
    );
  }
}
