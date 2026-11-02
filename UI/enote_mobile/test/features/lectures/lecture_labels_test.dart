import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/features/lectures/lecture_labels.dart';

void main() {
  test('rsvp state maps to label and tone', () {
    expect(rsvpStateLabel(AttendanceStatus.present), 'Prisustvo potvrđeno');
    expect(rsvpStateTone(AttendanceStatus.present), StatusTone.success);
    expect(rsvpStateLabel(AttendanceStatus.absent), 'Prisustvo odbijeno');
    expect(rsvpStateTone(AttendanceStatus.absent), StatusTone.neutral);
    expect(rsvpStateLabel(null), 'Niste odgovorili');
    expect(rsvpStateTone(null), StatusTone.warning);
  });
}
