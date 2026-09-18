import 'enums.dart';

/// What a payment screen needs from a stored payment row, whichever
/// endpoint it came from (`RentalPaymentDto`, `CoursePaymentDto`).
abstract interface class PaymentRecord {
  int get amountCents;
  String get currency;
  PaymentStatus get status;
  DateTime? get paidAt;
}
