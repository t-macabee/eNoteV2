import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

class StatusChip extends StatelessWidget {
  final String label;
  final Color color;

  const StatusChip({super.key, required this.label, required this.color});

  factory StatusChip.rental(InstrumentRentalStatus status) {
    return switch (status) {
      InstrumentRentalStatus.pending => const StatusChip(
        label: 'Na čekanju',
        color: AppTheme.warning,
      ),
      InstrumentRentalStatus.approved => const StatusChip(
        label: 'Odobreno',
        color: AppTheme.primary,
      ),
      InstrumentRentalStatus.active => const StatusChip(
        label: 'Aktivno',
        color: AppTheme.success,
      ),
      InstrumentRentalStatus.completed => const StatusChip(
        label: 'Završeno',
        color: AppTheme.textSecondary,
      ),
      InstrumentRentalStatus.rejected => const StatusChip(
        label: 'Odbijeno',
        color: AppTheme.error,
      ),
      InstrumentRentalStatus.canceled => const StatusChip(
        label: 'Otkazano',
        color: AppTheme.error,
      ),
      InstrumentRentalStatus.returnedEarly => const StatusChip(
        label: 'Prijevremeni povrat',
        color: AppTheme.textSecondary,
      ),
    };
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
    return switch (status) {
      LectureStatus.scheduled => const StatusChip(
        label: 'Zakazano',
        color: AppTheme.primary,
      ),
      LectureStatus.held => const StatusChip(
        label: 'Održano',
        color: AppTheme.textSecondary,
      ),
      LectureStatus.cancelled => const StatusChip(
        label: 'Otkazano',
        color: AppTheme.error,
      ),
    };
  }

  factory StatusChip.lectureType(LectureType type) {
    return switch (type) {
      LectureType.theoretical => const StatusChip(
        label: 'Teorijsko',
        color: AppTheme.textSecondary,
      ),
      LectureType.practical => const StatusChip(
        label: 'Praktično',
        color: AppTheme.textSecondary,
      ),
      LectureType.combined => const StatusChip(
        label: 'Kombinovano',
        color: AppTheme.textSecondary,
      ),
    };
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
