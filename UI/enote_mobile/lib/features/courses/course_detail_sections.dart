import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/labeled_value.dart';
import '../lectures/lecture_labels.dart';

String lockedContentReason(int lectureCount) =>
    'Predavanja ($lectureCount) su privremeno nedostupna. '
    'Obnovite školarinu da biste nastavili.';

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
  final VoidCallback onTuitionHistory;

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
    required this.onTuitionHistory,
  });

  @override
  Widget build(BuildContext context) {
    final status = course.enrollmentStatus;
    final canRequest =
        status == null ||
        status == EnrollmentStatus.canceled ||
        status == EnrollmentStatus.rejected;
    final lockedContent = enrolled && !course.hasContentAccess;
    final rankingButton = TextButton(
      onPressed: course.hasContentAccess ? onRanking : null,
      child: const Text('Rang lista'),
    );
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
                StatusChip(
                  label: enrollmentStatusLabel(status),
                  tone: enrollmentStatusTone(status),
                ),
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
            const StatusBanner(
              icon: Icons.hourglass_empty,
              message: 'Zahtjev za upis čeka odobrenje instruktora.',
              tone: StatusTone.warning,
            ),
          ],
          if (status == EnrollmentStatus.rejected) ...[
            const SizedBox(height: 8),
            StatusBanner(
              icon: Icons.info_outline,
              message:
                  'Zahtjev za upis je odbijen: '
                  '${course.enrollmentDecisionNote ?? '-'}',
              tone: StatusTone.danger,
            ),
          ],
          if (status == EnrollmentStatus.canceled &&
              course.hasContentAccess) ...[
            const SizedBox(height: 8),
            StatusBanner(
              icon: Icons.event_available,
              message:
                  'Ispisani ste sa kursa. Pristup traje do '
                  '${formatDate(course.paidUntil!)}',
              tone: StatusTone.info,
            ),
          ],
          if (status == EnrollmentStatus.completed) ...[
            const SizedBox(height: 8),
            const StatusBanner(
              icon: Icons.check_circle_outline,
              message: 'Kurs ste završili. Ponovni upis nije moguć.',
              tone: StatusTone.success,
            ),
          ],
          if (course.hasEnded && canRequest) ...[
            const SizedBox(height: 8),
            const StatusBanner(
              icon: Icons.event_busy,
              message: 'Kurs je završen. Upis više nije moguć.',
              tone: StatusTone.neutral,
            ),
          ],
          if (membershipBlocked && canRequest) ...[
            const SizedBox(height: 8),
            StatusBanner(
              icon: Icons.info_outline,
              message: membershipPaidUntil == null
                  ? 'Članarina nije aktivna.'
                  : 'Članarina je istekla ${formatDate(membershipPaidUntil!)} '
                        'Obratite se školi za obnovu.',
              tone: StatusTone.danger,
            ),
          ],
          if (enrolled && !course.isFree) ...[
            const SizedBox(height: 8),
            _TuitionBanner(
              course: course,
              onTuition: onTuition,
              onHistory: course.enrollmentId == null ? null : onTuitionHistory,
            ),
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
              else if (status == EnrollmentStatus.canceled &&
                  course.hasContentAccess)
                FilledButton(
                  onPressed: (membershipBlocked || isActing || course.hasEnded)
                      ? null
                      : onEnroll,
                  child: const Text('Nastavi pretplatu'),
                )
              else
                FilledButton(
                  onPressed:
                      (membershipBlocked ||
                          isActing ||
                          course.hasEnded ||
                          status == EnrollmentStatus.completed)
                      ? null
                      : onEnroll,
                  child: const Text('Upiši se'),
                ),
              const SizedBox(width: 8),
              if (!course.hasContentAccess)
                Tooltip(
                  message: lockedContent
                      ? lockedContentReason(course.lectureCount)
                      : 'Rang lista je dostupna polaznicima s pristupom sadržaju.',
                  child: rankingButton,
                )
              else
                rankingButton,
            ],
          ),
        ],
      ),
    );
  }
}

class _TuitionBanner extends StatelessWidget {
  final CourseDto course;
  final VoidCallback onTuition;
  final VoidCallback? onHistory;

  const _TuitionBanner({
    required this.course,
    required this.onTuition,
    required this.onHistory,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _status(),
        if (onHistory != null)
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton.icon(
              onPressed: onHistory,
              icon: const Icon(Icons.receipt_long_outlined, size: 18),
              label: const Text('Historija uplata'),
            ),
          ),
      ],
    );
  }

  Widget _status() {
    final paidUntil = course.paidUntil;
    if (paidUntil == null) {
      return StatusBanner(
        icon: Icons.info_outline,
        message: 'Školarina nije plaćena.',
        tone: StatusTone.warning,
        actionLabel: 'Plati',
        onAction: onTuition,
      );
    }
    if (paidUntil.isBefore(DateTime.now())) {
      return StatusBanner(
        icon: Icons.warning_amber_outlined,
        message: 'Školarina je istekla ${formatDate(paidUntil)}',
        tone: StatusTone.danger,
        actionLabel: 'Obnovi',
        onAction: onTuition,
      );
    }
    return LabeledValue(label: 'Plaćeno do', value: formatDate(paidUntil));
  }
}

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
                  Expanded(child: Text(title.toUpperCase(), style: textStyle)),
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
            StatusChip(
              label: lectureStatusLabel(lecture.lectureStatus),
              tone: lectureStatusTone(lecture.lectureStatus),
            ),
          ],
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              '${lecture.name} · '
              '${lectureTypeLabel(lecture.lectureType)} · '
              '${lecture.location}',
            ),
            const SizedBox(height: 4),
            StatusChip(
              label: rsvpStateLabel(lecture.myAttendanceStatus),
              tone: rsvpStateTone(lecture.myAttendanceStatus),
            ),
          ],
        ),
        isThreeLine: true,
        trailing: const Icon(Icons.chevron_right),
        onTap: () => onTap(),
      ),
    );
  }
}
