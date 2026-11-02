import 'package:enote_core/enote_core.dart';

({String label, StatusTone tone}) assignmentStatus({
  required DateTime dueAt,
  DateTime? submittedAt,
  int? grade,
  required DateTime now,
}) {
  if (grade != null) {
    return (label: 'Ocjena: $grade', tone: StatusTone.success);
  }
  if (submittedAt != null) {
    return (label: 'Predano', tone: StatusTone.info);
  }
  if (now.isAfter(dueAt)) {
    return (label: 'Rok istekao', tone: StatusTone.danger);
  }
  return (label: 'Nije predano', tone: StatusTone.warning);
}
