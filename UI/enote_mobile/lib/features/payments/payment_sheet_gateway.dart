import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';

abstract class PaymentSheetGateway {
  Future<void> init(String clientSecret);

  Future<void> present();
}

class PaymentSheetCancelled implements Exception {}

class PaymentSheetFailure implements Exception {
  final String message;

  PaymentSheetFailure(this.message);
}

class StripePaymentSheetGateway implements PaymentSheetGateway {
  @override
  Future<void> init(String clientSecret) {
    return Stripe.instance.initPaymentSheet(
      paymentSheetParameters: SetupPaymentSheetParameters(
        paymentIntentClientSecret: clientSecret,
        merchantDisplayName: 'eNote',
        returnURL: 'enote://stripe-redirect',
        style: ThemeMode.dark,
      ),
    );
  }

  @override
  Future<void> present() async {
    try {
      await Stripe.instance.presentPaymentSheet();
    } on StripeException catch (e) {
      if (e.error.code == FailureCode.Canceled) {
        throw PaymentSheetCancelled();
      }
      throw PaymentSheetFailure(
        e.error.localizedMessage ?? e.error.message ?? 'Plaćanje nije dovršeno',
      );
    }
  }
}
