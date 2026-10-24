bool isMembershipActive(DateTime? paidUntil, {DateTime? now}) {
  if (paidUntil == null) return false;
  final until = paidUntil.toUtc();
  final current = (now ?? DateTime.now()).toUtc();
  final untilDay = DateTime.utc(until.year, until.month, until.day);
  final currentDay = DateTime.utc(current.year, current.month, current.day);
  return !untilDay.isBefore(currentDay);
}

bool isMembershipWarningAllowed(DateTime? paidUntil, {DateTime? now}) {
  if (paidUntil == null) return true;
  final current = (now ?? DateTime.now()).toUtc();
  final today = DateTime.utc(current.year, current.month, current.day);
  final until = paidUntil.toUtc();
  final untilDay = DateTime.utc(until.year, until.month, until.day);
  return !untilDay.isAfter(today.add(const Duration(days: 7)));
}
