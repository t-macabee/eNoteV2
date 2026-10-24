import '../../formatting/formatters.dart';

mixin PaymentIntentDraft {
  String get clientSecret;
  int get amountCents;
  String get currency;

  String get amountLabel => formatKM(amountCents / 100, currency: currency);
}
