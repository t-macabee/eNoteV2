import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/features/lectures/lecture_labels.dart';

LectureDto _lecture({
  bool individual = false,
  LectureStatus status = LectureStatus.scheduled,
  AttendanceStatus? rsvp,
}) => LectureDto(
  id: 11,
  studentId: individual ? 7 : null,
  name: 'Akordi I',
  location: 'Sala 2',
  lectureType: LectureType.theoretical,
  lectureStatus: status,
  isCancelled: status == LectureStatus.cancelled,
  lectureTime: DateTime.now(),
  duration: 60,
  capacity: 20,
  attendeeCount: 5,
  myAttendanceStatus: rsvp,
);

List<StatusChip> _chips(LectureDto lecture) =>
    lectureStateChips(lecture).cast<StatusChip>();

void main() {
  test('rsvp state maps to label and tone', () {
    expect(rsvpStateLabel(AttendanceStatus.present), 'Prisustvo potvrđeno');
    expect(rsvpStateTone(AttendanceStatus.present), StatusTone.success);
    expect(rsvpStateLabel(AttendanceStatus.absent), 'Prisustvo odbijeno');
    expect(rsvpStateTone(AttendanceStatus.absent), StatusTone.neutral);
    expect(rsvpStateLabel(null), 'Niste odgovorili');
    expect(rsvpStateTone(null), StatusTone.warning);
  });

  test('state chips hide the RSVP chip for a held lecture', () {
    final chips = _chips(
      _lecture(status: LectureStatus.held, rsvp: AttendanceStatus.pending),
    );

    expect(
      chips.map((chip) => chip.label),
      isNot(contains('Niste odgovorili')),
    );
  });

  test('state chips show Individualni čas with the accent tone', () {
    final chips = _chips(
      _lecture(individual: true, status: LectureStatus.held),
    );

    expect(chips.single.label, 'Individualni čas');
    expect(chips.single.tone, StatusTone.accent);
  });
}
