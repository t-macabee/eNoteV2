import 'enums.dart';

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
