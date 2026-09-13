import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class StatusChip extends StatelessWidget {
  final String label;
  final Color color;

  const StatusChip({super.key, required this.label, required this.color});

  factory StatusChip.rental(InstrumentRentalStatus status) {
    return StatusChip(
      label: rentalStatusLabel(status),
      color: switch (status) {
        InstrumentRentalStatus.pending => AppTheme.warning,
        InstrumentRentalStatus.approved => AppTheme.primary,
        InstrumentRentalStatus.active => AppTheme.success,
        InstrumentRentalStatus.completed => AppTheme.textSecondary,
        InstrumentRentalStatus.rejected => AppTheme.error,
        InstrumentRentalStatus.canceled => AppTheme.error,
        InstrumentRentalStatus.returnedEarly => AppTheme.textSecondary,
      },
    );
  }

  factory StatusChip.payment(PaymentStatus status) {
    return switch (status) {
      PaymentStatus.requiresAction => const StatusChip(
        label: 'Čeka plaćanje',
        color: AppTheme.warning,
      ),
      PaymentStatus.succeeded => const StatusChip(
        label: 'Plaćeno',
        color: AppTheme.success,
      ),
      PaymentStatus.failed => const StatusChip(
        label: 'Neuspješno',
        color: AppTheme.error,
      ),
      PaymentStatus.canceled => const StatusChip(
        label: 'Otkazano',
        color: AppTheme.error,
      ),
      PaymentStatus.refunded => const StatusChip(
        label: 'Puni povrat',
        color: AppTheme.textSecondary,
      ),
      PaymentStatus.partiallyRefunded => const StatusChip(
        label: 'Djelomični povrat',
        color: AppTheme.textSecondary,
      ),
    };
  }

  factory StatusChip.lectureStatus(LectureStatus status) {
    return StatusChip(
      label: lectureStatusLabel(status),
      color: switch (status) {
        LectureStatus.scheduled => AppTheme.primary,
        LectureStatus.held => AppTheme.textSecondary,
        LectureStatus.cancelled => AppTheme.error,
      },
    );
  }

  factory StatusChip.lectureType(LectureType type) {
    return StatusChip(
      label: lectureTypeLabel(type),
      color: switch (type) {
        LectureType.theoretical => AppTheme.textSecondary,
        LectureType.practical => AppTheme.textSecondary,
        LectureType.combined => AppTheme.textSecondary,
      },
    );
  }

  factory StatusChip.attendance(AttendanceStatus? status) {
    return switch (status) {
      AttendanceStatus.present => const StatusChip(
        label: 'Prisustvo potvrđeno',
        color: AppTheme.success,
      ),
      AttendanceStatus.absent => const StatusChip(
        label: 'Prisustvo odbijeno',
        color: AppTheme.error,
      ),
      _ => const StatusChip(
        label: 'Niste odgovorili',
        color: AppTheme.textSecondary,
      ),
    };
  }

  @override
  Widget build(BuildContext context) {
    return Chip(
      label: Text(label, style: TextStyle(color: color)),
      backgroundColor: color.withValues(alpha: 0.15),
      side: BorderSide.none,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
  }
}
