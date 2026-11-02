import 'package:enote_core/enote_core.dart';

({String label, StatusTone tone}) assignmentDeadlineStatus(
  DateTime dueAt,
  DateTime now,
) => now.isAfter(dueAt)
    ? (label: 'Rok istekao', tone: StatusTone.danger)
    : (label: 'Otvoreno', tone: StatusTone.info);
