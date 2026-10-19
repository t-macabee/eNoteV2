import 'package:enote_core/enote_core.dart';

String rsvpStateLabel(AttendanceStatus? status) => switch (status) {
  AttendanceStatus.present => 'Prisustvo potvrđeno',
  AttendanceStatus.absent => 'Prisustvo odbijeno',
  _ => 'Niste odgovorili',
};
