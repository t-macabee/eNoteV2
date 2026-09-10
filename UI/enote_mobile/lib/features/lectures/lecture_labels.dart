import 'package:enote_core/enote_core.dart';

/// Inline text labels for lecture enums (02 §8.3). Chips live in
/// `StatusChip`; these are for running text (rows, detail lines).
String lectureTypeLabel(LectureType type) => switch (type) {
  LectureType.theoretical => 'Teorijsko',
  LectureType.practical => 'Praktično',
  LectureType.combined => 'Kombinovano',
};

String lectureStatusLabel(LectureStatus status) => switch (status) {
  LectureStatus.scheduled => 'Zakazano',
  LectureStatus.held => 'Održano',
  LectureStatus.cancelled => 'Otkazano',
};

String rsvpStateLabel(AttendanceStatus? status) => switch (status) {
  AttendanceStatus.present => 'Prisustvo potvrđeno',
  AttendanceStatus.absent => 'Prisustvo odbijeno',
  _ => 'Niste odgovorili',
};
