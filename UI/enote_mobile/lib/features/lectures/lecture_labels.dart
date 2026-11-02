import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

String rsvpStateLabel(AttendanceStatus? status) => switch (status) {
  AttendanceStatus.present => 'Prisustvo potvrđeno',
  AttendanceStatus.absent => 'Prisustvo odbijeno',
  _ => 'Niste odgovorili',
};

StatusTone rsvpStateTone(AttendanceStatus? status) => switch (status) {
  AttendanceStatus.present => StatusTone.success,
  AttendanceStatus.absent => StatusTone.neutral,
  _ => StatusTone.warning,
};

List<Widget> lectureStateChips(LectureDto lecture) => [
  if (lecture.isIndividual)
    const StatusChip(label: 'Individualni čas', tone: StatusTone.accent),
  if (lecture.lectureStatus == LectureStatus.scheduled)
    StatusChip(
      label: rsvpStateLabel(lecture.myAttendanceStatus),
      tone: rsvpStateTone(lecture.myAttendanceStatus),
    ),
];
