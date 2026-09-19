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

/// Formats a money amount as `X.XX <currency>` (default `KM`), or an em dash
/// when null.
///
/// Use only for money amounts. Several
/// `toStringAsFixed(2)` sites are not money (grades, form-controller text)
/// or carry the unit in a column header already — adding ` KM` there would
/// duplicate the unit.
String formatKM(double? value, {String currency = 'KM'}) {
  if (value == null) return '—';
  return '${value.toStringAsFixed(2)} ${_currencyLabel(currency)}';
}

// Currency display contract, mirrored in
// eNote/eNote.Infrastructure/Messaging/RentalNotificationDispatcher.cs
// (FormatCurrency): an empty code or the BAM code is displayed as KM;
// every other code is upper-cased.
const _localCurrencyCode = 'bam';
const _localCurrencyLabel = 'KM';

String _currencyLabel(String currency) {
  if (currency.isEmpty || currency.toLowerCase() == _localCurrencyCode) {
    return _localCurrencyLabel;
  }
  return currency.toUpperCase();
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

/// Formats a succeeded payment's amount and, when known, its paid date,
/// joined with ` · ` — the shared D-state detail line for `PaymentStatusView`
/// across the rental and tuition payment screens.
String formatPaymentDetail(int amountCents, DateTime? paidAt, {String currency = 'KM'}) {
  final parts = <String>[formatKM(amountCents / 100, currency: currency)];
  if (paidAt != null) parts.add(formatDateTime(paidAt));
  return parts.join(' · ');
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
