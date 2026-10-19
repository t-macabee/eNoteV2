import 'enums.dart';

abstract interface class PaymentRecord {
  int get amountCents;
  String get currency;
  PaymentStatus get status;
  DateTime? get paidAt;
}
