import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/blocked_reason_banner.dart';
import '../../widgets/labeled_value.dart';
import '../lectures/lecture_labels.dart';

class CourseMasterCard extends StatelessWidget {
  final CourseDto course;
  final bool enrolled;
  final bool membershipBlocked;
  final DateTime? membershipPaidUntil;
  final bool isActing;
  final VoidCallback onEnroll;
  final VoidCallback onUnenroll;
  final VoidCallback onRanking;
  final VoidCallback onTuition;

  const CourseMasterCard({
    super.key,
    required this.course,
    required this.enrolled,
    required this.membershipBlocked,
    required this.membershipPaidUntil,
    required this.isActing,
    required this.onEnroll,
    required this.onUnenroll,
    required this.onRanking,
    required this.onTuition,
  });

  @override
  Widget build(BuildContext context) {
    final status = course.enrollmentStatus;
    final canRequest = status == null ||
        status == EnrollmentStatus.canceled ||
        status == EnrollmentStatus.rejected;
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  course.name,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
              if (status != null && status != EnrollmentStatus.canceled)
                Chip(label: Text(enrollmentStatusLabel(status))),
            ],
          ),
          const SizedBox(height: 12),
          LabeledValue(
            label: 'Instruktor',
            value: orDash(course.instructorName),
          ),
          LabeledValue(
            label: 'Trajanje',
            value:
                '${formatDateNullable(course.startDate)} – '
                '${formatDateNullable(course.endDate)}',
          ),
          LabeledValue(label: 'Mjesečna cijena', value: formatKM(course.price)),
          LabeledValue(
            label: 'Polaznika',
            value: course.enrolledCount.toString(),
          ),
          if (course.description != null) ...[
            const SizedBox(height: 8),
            Text(
              course.description!,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ],
          if (status == EnrollmentStatus.pending) ...[
            const SizedBox(height: 8),
            const BlockedReasonBanner(
              icon: Icons.hourglass_empty,
              reason: 'Zahtjev za upis čeka odobrenje instruktora.',
            ),
          ],
          if (status == EnrollmentStatus.rejected) ...[
            const SizedBox(height: 8),
            BlockedReasonBanner(
              icon: Icons.info_outline,
              reason:
                  'Zahtjev za upis je odbijen: '
                  '${course.enrollmentDecisionNote ?? '-'}',
            ),
          ],
          if (status == EnrollmentStatus.completed) ...[
            const SizedBox(height: 8),
            const BlockedReasonBanner(
              icon: Icons.check_circle_outline,
              reason: 'Kurs ste završili. Ponovni upis nije moguć.',
            ),
          ],
          if (membershipBlocked && canRequest) ...[
            const SizedBox(height: 8),
            BlockedReasonBanner(
              icon: Icons.info_outline,
              reason: membershipPaidUntil == null
                  ? 'Članarina nije aktivna.'
                  : 'Članarina je istekla ${formatDate(membershipPaidUntil!)} '
                        'Obratite se školi za obnovu.',
            ),
          ],
          if (enrolled && !course.isFree) ...[
            const SizedBox(height: 8),
            _TuitionBanner(course: course, onTuition: onTuition),
          ],
          const SizedBox(height: 12),
          Row(
            children: [
              if (status == EnrollmentStatus.active)
                OutlinedButton(
                  onPressed: isActing ? null : onUnenroll,
                  child: const Text('Ispiši se'),
                )
              else if (status == EnrollmentStatus.pending)
                OutlinedButton(
                  onPressed: isActing ? null : onUnenroll,
                  child: const Text('Otkaži zahtjev'),
                )
              else
                FilledButton(
                  onPressed:
                      (membershipBlocked ||
                          isActing ||
                          status == EnrollmentStatus.completed)
                      ? null
                      : onEnroll,
                  child: const Text('Upiši se'),
                ),
              const SizedBox(width: 8),
              TextButton(
                onPressed: onRanking,
                child: const Text('Rang lista'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Enrolled only: an unpaid/expired period gets a banner with the renewal
/// action, a paid one gets a plain reminder row, a free course gets nothing.
class _TuitionBanner extends StatelessWidget {
  final CourseDto course;
  final VoidCallback onTuition;

  const _TuitionBanner({required this.course, required this.onTuition});

  @override
  Widget build(BuildContext context) {
    final paidUntil = course.paidUntil;
    if (paidUntil == null) {
      return BlockedReasonBanner(
        icon: Icons.info_outline,
        reason: 'Školarina nije plaćena.',
        actionLabel: 'Plati',
        onAction: onTuition,
      );
    }
    if (paidUntil.isBefore(DateTime.now())) {
      return BlockedReasonBanner(
        icon: Icons.warning_amber_outlined,
        reason: 'Školarina je istekla ${formatDate(paidUntil)}',
        actionLabel: 'Obnovi',
        onAction: onTuition,
      );
    }
    return LabeledValue(
      label: 'Plaćeno do',
      value: formatDate(paidUntil),
    );
  }
}

/// Pinned lectures section header: `SectionHeader` copy with a 🔍 action
/// that reveals the lectures `name` search field below it.
class CourseLecturesHeader extends SliverPersistentHeaderDelegate {
  final String title;
  final bool showSearch;
  final VoidCallback onToggleSearch;
  final TextEditingController searchController;
  final Color backgroundColor;
  final TextStyle? textStyle;

  const CourseLecturesHeader({
    required this.title,
    required this.showSearch,
    required this.onToggleSearch,
    required this.searchController,
    required this.backgroundColor,
    required this.textStyle,
  });

  @override
  double get minExtent => showSearch ? 112 : 48;

  @override
  double get maxExtent => showSearch ? 112 : 48;

  @override
  Widget build(
    BuildContext context,
    double shrinkOffset,
    bool overlapsContent,
  ) {
    // Fixed-height equivalent of SectionHeader (uppercase labelSmall +
    // trailing action): SectionHeader's own vertical padding does not fit a
    // 48 px pinned extent, so the row is laid out to exactly 48 px here.
    return Container(
      color: backgroundColor,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            height: 48,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Row(
                children: [
                  Expanded(
                    child: Text(title.toUpperCase(), style: textStyle),
                  ),
                  IconButton(
                    icon: const Icon(Icons.search_outlined),
                    onPressed: onToggleSearch,
                  ),
                ],
              ),
            ),
          ),
          if (showSearch)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: TextField(
                controller: searchController,
                decoration: const InputDecoration(
                  hintText: 'Pretraži…',
                  prefixIcon: Icon(Icons.search_outlined),
                ),
              ),
            ),
        ],
      ),
    );
  }

  @override
  bool shouldRebuild(CourseLecturesHeader oldDelegate) =>
      title != oldDelegate.title ||
      showSearch != oldDelegate.showSearch ||
      backgroundColor != oldDelegate.backgroundColor ||
      textStyle != oldDelegate.textStyle ||
      // `onToggleSearch` is deliberately not compared: it is an inline
      // closure with a fresh identity on every parent build, so comparing it
      // would make the delegate rebuild unconditionally.
      searchController != oldDelegate.searchController;
}

class CourseLectureRow extends StatelessWidget {
  final LectureDto lecture;
  final Future<void> Function() onTap;

  const CourseLectureRow({
    super.key,
    required this.lecture,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ListTile(
        title: Row(
          children: [
            Expanded(
              child: Text(
                formatDateTime(lecture.lectureTime),
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
            Text(
              lectureStatusLabel(lecture.lectureStatus),
              style: TextStyle(
                color: lecture.lectureStatus == LectureStatus.cancelled
                    ? Theme.of(context).colorScheme.error
                    : Theme.of(context).textTheme.bodyMedium?.color,
              ),
            ),
          ],
        ),
        subtitle: Text(
          '${lecture.name} · '
          '${lectureTypeLabel(lecture.lectureType)} · '
          '${lecture.location}\n'
          '${rsvpStateLabel(lecture.myAttendanceStatus)}',
        ),
        isThreeLine: true,
        trailing: const Icon(Icons.chevron_right),
        onTap: () => onTap(),
      ),
    );
  }
}
