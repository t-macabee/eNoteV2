String formatDate(DateTime date) {
  final day = date.day.toString().padLeft(2, '0');
  final month = date.month.toString().padLeft(2, '0');
  return '$day.$month.${date.year}.';
}

String formatDateNullable(DateTime? date) {
  if (date == null) return '-';
  return formatDate(date);
}

String formatDateTime(DateTime date) {
  final day = date.day.toString().padLeft(2, '0');
  final month = date.month.toString().padLeft(2, '0');
  final hour = date.hour.toString().padLeft(2, '0');
  final minute = date.minute.toString().padLeft(2, '0');
  return '$day.$month.${date.year}. $hour:$minute';
}

String formatTime(DateTime date) {
  final hour = date.hour.toString().padLeft(2, '0');
  final minute = date.minute.toString().padLeft(2, '0');
  return '$hour:$minute';
}

String formatDayTime(DateTime date) {
  final day = date.day.toString().padLeft(2, '0');
  final month = date.month.toString().padLeft(2, '0');
  final hour = date.hour.toString().padLeft(2, '0');
  final minute = date.minute.toString().padLeft(2, '0');
  return '$day.$month. $hour:$minute';
}

String formatDisplayName(String? firstName, String? lastName, String? username) {
  final name = '${firstName ?? ''} ${lastName ?? ''}'.trim();
  if (name.isNotEmpty) return name;
  return username ?? '-';
}

/// Formats a money amount as `X.XX KM`, or an em dash when null.
///
/// Use only for amounts displayed with a `KM` suffix today. Several
/// `toStringAsFixed(2)` sites are not money (grades, form-controller text)
/// or carry the unit in a column header already — adding ` KM` there would
/// duplicate the unit.
String formatKM(double? value) {
  if (value == null) return '—';
  return '${value.toStringAsFixed(2)} KM';
}

/// Returns [s], or an em dash when null or empty.
String orDash(String? s) {
  if (s == null || s.isEmpty) return '—';
  return s;
}

String truncate(String text, int maxLength) {
  if (text.length <= maxLength) return text;
  return '${text.substring(0, maxLength)}…';
}

DateTime? parseDate(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  if (value is String) {
    try {
      return DateTime.parse(value);
    } catch (_) {
      return null;
    }
  }
  return null;
}
