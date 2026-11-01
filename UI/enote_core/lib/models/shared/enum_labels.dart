import '../../theme/app_tokens.dart';
import 'enums.dart';

StatusTone rentalStatusTone(InstrumentRentalStatus status) => switch (status) {
  InstrumentRentalStatus.pending => StatusTone.warning,
  InstrumentRentalStatus.approved => StatusTone.info,
  InstrumentRentalStatus.active => StatusTone.success,
  InstrumentRentalStatus.completed => StatusTone.neutral,
  InstrumentRentalStatus.rejected => StatusTone.danger,
  InstrumentRentalStatus.canceled => StatusTone.neutral,
  InstrumentRentalStatus.returnedEarly => StatusTone.accent,
};

StatusTone enrollmentStatusTone(EnrollmentStatus status) => switch (status) {
  EnrollmentStatus.pending => StatusTone.warning,
  EnrollmentStatus.active => StatusTone.success,
  EnrollmentStatus.completed => StatusTone.info,
  EnrollmentStatus.rejected => StatusTone.danger,
  EnrollmentStatus.canceled => StatusTone.neutral,
};

StatusTone paymentStatusTone(PaymentStatus status) => switch (status) {
  PaymentStatus.requiresAction => StatusTone.warning,
  PaymentStatus.succeeded => StatusTone.success,
  PaymentStatus.failed => StatusTone.danger,
  PaymentStatus.canceled => StatusTone.neutral,
  PaymentStatus.refunded => StatusTone.accent,
  PaymentStatus.partiallyRefunded => StatusTone.accent,
};

String paymentStatusLabel(PaymentStatus status) => switch (status) {
  PaymentStatus.requiresAction => 'Čeka uplatu',
  PaymentStatus.succeeded => 'Plaćeno',
  PaymentStatus.failed => 'Neuspjelo',
  PaymentStatus.canceled => 'Otkazano',
  PaymentStatus.refunded => 'Vraćeno',
  PaymentStatus.partiallyRefunded => 'Djelimično vraćeno',
};

String rentalStatusLabel(InstrumentRentalStatus status) => switch (status) {
  InstrumentRentalStatus.pending => 'Na čekanju',
  InstrumentRentalStatus.approved => 'Odobreno',
  InstrumentRentalStatus.active => 'Aktivno',
  InstrumentRentalStatus.completed => 'Završeno',
  InstrumentRentalStatus.rejected => 'Odbijeno',
  InstrumentRentalStatus.canceled => 'Otkazano',
  InstrumentRentalStatus.returnedEarly => 'Prijevremeni povrat',
};

String enrollmentStatusLabel(EnrollmentStatus status) => switch (status) {
  EnrollmentStatus.active => 'Upisan',
  EnrollmentStatus.pending => 'Na čekanju',
  EnrollmentStatus.rejected => 'Odbijeno',
  EnrollmentStatus.canceled => 'Otkazano',
  EnrollmentStatus.completed => 'Položen',
};

String lectureStatusLabel(LectureStatus status) => switch (status) {
  LectureStatus.scheduled => 'Zakazano',
  LectureStatus.held => 'Održano',
  LectureStatus.cancelled => 'Otkazano',
};

String lectureTypeLabel(LectureType type) => switch (type) {
  LectureType.theoretical => 'Teorijsko',
  LectureType.practical => 'Praktično',
  LectureType.combined => 'Kombinovano',
};

String userRoleLabel(UserRole role) => switch (role) {
  UserRole.administrator => 'Administrator',
  UserRole.instructor => 'Instruktor',
  UserRole.student => 'Student',
  UserRole.storeEmployee => 'Uposlenik prodavnice',
};
