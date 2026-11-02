import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';
import '../../widgets/detail_actions.dart';
import '../../widgets/detail_header.dart';
import '../../widgets/detail_section.dart';
import '../../widgets/labeled_value.dart';
import '../student_notices.dart';

String lockedContentReason(int lectureCount) =>
    'Predavanja ($lectureCount) su privremeno nedostupna. '
    'Obnovite školarinu da biste nastavili.';

class CourseDetailSummary extends StatelessWidget {
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

  const CourseDetailSummary({
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
    final paidUntil = course.paidUntil;
    final tuitionUnpaid = enrolled && !course.isFree && paidUntil == null;
    final tuitionExpired =
        enrolled &&
        !course.isFree &&
        paidUntil != null &&
        paidUntil.isBefore(DateTime.now());
    final tuitionPaid =
        enrolled && !course.isFree && !tuitionExpired && paidUntil != null;
    final lockedContent = enrolled && !course.hasContentAccess;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        DetailHeader(
          title: course.name,
          subtitle: orDash(course.instructorName),
          chips: [
            if (status != null && status != EnrollmentStatus.canceled)
              StatusChip(
                label: enrollmentStatusLabel(status),
                tone: enrollmentStatusTone(status),
              ),
          ],
        ),
        DetailSection(
          title: 'Detalji',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              LabeledValue(
                label: 'Trajanje',
                value:
                    '${formatDateNullable(course.startDate)} – '
                    '${formatDateNullable(course.endDate)}',
              ),
              LabeledValue(
                label: 'Mjesečna cijena',
                value: formatKM(course.price),
              ),
              LabeledValue(
                label: 'Polaznika',
                value: course.enrolledCount.toString(),
              ),
              if (tuitionPaid)
                LabeledValue(label: 'Plaćeno do', value: formatDate(paidUntil)),
              if (course.description != null) ...[
                const SizedBox(height: AppSpacing.sm),
                Text(
                  course.description!,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ],
          ),
        ),
        DetailActions(
          banner: _banners(
            status: status,
            canRequest: canRequest,
            lockedContent: lockedContent,
            tuitionExpired: tuitionExpired,
            tuitionUnpaid: tuitionUnpaid,
          ),
          actions: [
            ?_enrollAction(
              status: status,
              membershipBlocked: membershipBlocked,
            ),
            if (tuitionUnpaid || tuitionExpired || lockedContent)
              FilledButton(
                onPressed: onTuition,
                child: Text(tuitionExpired ? 'Obnovi' : 'Plati'),
              ),
            if (course.hasContentAccess)
              OutlinedButton(
                onPressed: onRanking,
                child: const Text('Rang lista'),
              ),
            if (enrolled && !course.isFree && course.enrollmentId != null)
              OutlinedButton(
                onPressed: onTuitionHistory,
                child: const Text('Historija uplata'),
              ),
            ?_destructiveAction(status),
          ],
        ),
      ],
    );
  }

  Widget? _enrollAction({
    required EnrollmentStatus? status,
    required bool membershipBlocked,
  }) {
    if (course.hasEnded) return null;
    if (status == EnrollmentStatus.canceled && course.hasContentAccess) {
      return FilledButton(
        onPressed: (membershipBlocked || isActing) ? null : onEnroll,
        child: const Text('Nastavi pretplatu'),
      );
    }
    if (status == EnrollmentStatus.active ||
        status == EnrollmentStatus.pending ||
        status == EnrollmentStatus.completed) {
      return null;
    }
    return FilledButton(
      onPressed: (membershipBlocked || isActing) ? null : onEnroll,
      child: const Text('Upiši se'),
    );
  }

  Widget? _destructiveAction(EnrollmentStatus? status) {
    if (status == EnrollmentStatus.active) {
      return OutlinedButton(
        onPressed: isActing ? null : onUnenroll,
        style: dangerOutlinedButtonStyle,
        child: const Text('Ispiši se'),
      );
    }
    if (status == EnrollmentStatus.pending) {
      return OutlinedButton(
        onPressed: isActing ? null : onUnenroll,
        style: dangerOutlinedButtonStyle,
        child: const Text('Otkaži zahtjev'),
      );
    }
    return null;
  }

  Widget? _banners({
    required EnrollmentStatus? status,
    required bool canRequest,
    required bool lockedContent,
    required bool tuitionExpired,
    required bool tuitionUnpaid,
  }) {
    final banners = <Widget>[
      if (status == EnrollmentStatus.pending)
        const StatusBanner(
          icon: Icons.hourglass_empty,
          message: 'Zahtjev za upis čeka odobrenje instruktora.',
          tone: StatusTone.warning,
        ),
      if (status == EnrollmentStatus.rejected)
        StatusBanner(
          icon: Icons.info_outline,
          message:
              'Zahtjev za upis je odbijen: '
              '${course.enrollmentDecisionNote ?? '-'}',
          tone: StatusTone.danger,
        ),
      if (status == EnrollmentStatus.canceled && course.hasContentAccess)
        StatusBanner(
          icon: Icons.event_available,
          message:
              'Ispisani ste sa kursa. Pristup traje do '
              '${formatDate(course.paidUntil!)}',
          tone: StatusTone.info,
        ),
      if (status == EnrollmentStatus.completed)
        const StatusBanner(
          icon: Icons.check_circle_outline,
          message: 'Kurs ste završili. Ponovni upis nije moguć.',
          tone: StatusTone.success,
        ),
      if (course.hasEnded && canRequest)
        const StatusBanner(
          icon: Icons.event_busy,
          message: 'Kurs je završen. Upis više nije moguć.',
          tone: StatusTone.neutral,
        ),
      if (membershipBlocked && canRequest)
        StatusBanner(
          icon: Icons.info_outline,
          message: membershipNotice(membershipPaidUntil),
          tone: StatusTone.danger,
        ),
      if (tuitionUnpaid)
        StatusBanner(
          icon: Icons.info_outline,
          message: tuitionDueNotice(course.paidUntil),
          tone: StatusTone.warning,
        ),
      if (tuitionExpired)
        StatusBanner(
          icon: Icons.warning_amber_outlined,
          message: tuitionDueNotice(course.paidUntil),
          tone: StatusTone.danger,
        ),
      if (lockedContent)
        StatusBanner(
          icon: Icons.lock_outline,
          message: lockedContentReason(course.lectureCount),
          tone: StatusTone.warning,
        ),
    ];
    if (banners.isEmpty) return null;
    if (banners.length == 1) return banners.single;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: AppSpacing.sm,
      children: banners,
    );
  }
}

class CourseLecturesHeader extends SliverPersistentHeaderDelegate {
  static const double collapsedExtent = 48;
  static const double expandedExtent = 112;

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
  double get minExtent => showSearch ? expandedExtent : collapsedExtent;

  @override
  double get maxExtent => showSearch ? expandedExtent : collapsedExtent;

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
            height: collapsedExtent,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
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
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                0,
                AppSpacing.lg,
                AppSpacing.sm,
              ),
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
