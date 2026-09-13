bool isMembershipActive(DateTime? paidUntil, {DateTime? now}) {
  if (paidUntil == null) return false;
  final until = paidUntil.toUtc();
  final current = (now ?? DateTime.now()).toUtc();
  final untilDay = DateTime.utc(until.year, until.month, until.day);
  final currentDay = DateTime.utc(current.year, current.month, current.day);
  return !untilDay.isBefore(currentDay);
}
