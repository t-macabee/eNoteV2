import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/theme/app_theme.dart';
import 'package:enote_mobile/widgets/status_chip.dart';

void main() {
  test('rental statuses map to labels and colors', () {
    expect(StatusChip.rental(InstrumentRentalStatus.pending).label, 'Na čekanju');
    expect(StatusChip.rental(InstrumentRentalStatus.pending).color, AppTheme.warning);
    expect(StatusChip.rental(InstrumentRentalStatus.approved).label, 'Odobreno');
    expect(StatusChip.rental(InstrumentRentalStatus.approved).color, AppTheme.primary);
    expect(StatusChip.rental(InstrumentRentalStatus.active).label, 'Aktivno');
    expect(StatusChip.rental(InstrumentRentalStatus.active).color, AppTheme.success);
    expect(StatusChip.rental(InstrumentRentalStatus.completed).label, 'Završeno');
    expect(StatusChip.rental(InstrumentRentalStatus.completed).color, AppTheme.textSecondary);
    expect(StatusChip.rental(InstrumentRentalStatus.rejected).label, 'Odbijeno');
    expect(StatusChip.rental(InstrumentRentalStatus.rejected).color, AppTheme.error);
    expect(StatusChip.rental(InstrumentRentalStatus.canceled).label, 'Otkazano');
    expect(StatusChip.rental(InstrumentRentalStatus.canceled).color, AppTheme.error);
    expect(StatusChip.rental(InstrumentRentalStatus.returnedEarly).label, 'Prijevremeni povrat');
    expect(StatusChip.rental(InstrumentRentalStatus.returnedEarly).color, AppTheme.textSecondary);
  });

  test('payment statuses map to labels and colors', () {
    expect(StatusChip.payment(PaymentStatus.requiresAction).label, 'Čeka plaćanje');
    expect(StatusChip.payment(PaymentStatus.requiresAction).color, AppTheme.warning);
    expect(StatusChip.payment(PaymentStatus.succeeded).label, 'Plaćeno');
    expect(StatusChip.payment(PaymentStatus.succeeded).color, AppTheme.success);
    expect(StatusChip.payment(PaymentStatus.failed).label, 'Neuspješno');
    expect(StatusChip.payment(PaymentStatus.failed).color, AppTheme.error);
    expect(StatusChip.payment(PaymentStatus.canceled).label, 'Otkazano');
    expect(StatusChip.payment(PaymentStatus.canceled).color, AppTheme.error);
    expect(StatusChip.payment(PaymentStatus.refunded).label, 'Puni povrat');
    expect(StatusChip.payment(PaymentStatus.refunded).color, AppTheme.textSecondary);
    expect(StatusChip.payment(PaymentStatus.partiallyRefunded).label, 'Djelomični povrat');
    expect(StatusChip.payment(PaymentStatus.partiallyRefunded).color, AppTheme.textSecondary);
  });

  test('lecture statuses and types map to labels and colors', () {
    expect(StatusChip.lectureStatus(LectureStatus.scheduled).label, 'Zakazano');
    expect(StatusChip.lectureStatus(LectureStatus.scheduled).color, AppTheme.primary);
    expect(StatusChip.lectureStatus(LectureStatus.held).label, 'Održano');
    expect(StatusChip.lectureStatus(LectureStatus.held).color, AppTheme.textSecondary);
    expect(StatusChip.lectureStatus(LectureStatus.cancelled).label, 'Otkazano');
    expect(StatusChip.lectureStatus(LectureStatus.cancelled).color, AppTheme.error);
    expect(StatusChip.lectureType(LectureType.theoretical).label, 'Teorijsko');
    expect(StatusChip.lectureType(LectureType.practical).label, 'Praktično');
    expect(StatusChip.lectureType(LectureType.combined).label, 'Kombinovano');
  });

  test('attendance maps to labels and colors', () {
    expect(StatusChip.attendance(null).label, 'Niste odgovorili');
    expect(StatusChip.attendance(null).color, AppTheme.textSecondary);
    expect(StatusChip.attendance(AttendanceStatus.pending).label, 'Niste odgovorili');
    expect(StatusChip.attendance(AttendanceStatus.present).label, 'Prisustvo potvrđeno');
    expect(StatusChip.attendance(AttendanceStatus.present).color, AppTheme.success);
    expect(StatusChip.attendance(AttendanceStatus.absent).label, 'Prisustvo odbijeno');
    expect(StatusChip.attendance(AttendanceStatus.absent).color, AppTheme.error);
  });

  testWidgets('renders its label', (tester) async {
    await tester.pumpWidget(
      const _Harness(child: StatusChip(label: 'Na čekanju', color: AppTheme.warning)),
    );
    expect(find.text('Na čekanju'), findsOneWidget);
  });
}

class _Harness extends StatelessWidget {
  final Widget child;

  const _Harness({required this.child});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(home: Scaffold(body: child));
  }
}
