import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';

/// The one interface in the app (03 PD5): a two-method abstraction over
/// `Stripe.instance` so every payment state in 02 §10 is widget-testable
/// without the Stripe SDK.
abstract class PaymentSheetGateway {
  /// Initialises the sheet for [clientSecret] (fresh per attempt, 02 §10.4).
  Future<void> init(String clientSecret);

  /// Presents the sheet. Resolves when the sheet completes; throws
  /// [PaymentSheetCancelled] when the student dismisses it and
  /// [PaymentSheetFailure] on a provider error.
  Future<void> present();
}

/// The student dismissed the sheet before paying.
class PaymentSheetCancelled implements Exception {}

/// The sheet reported a provider error (decline, misconfiguration, …).
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
      // Verified against flutter_stripe 14.0.0 (`stripe_platform_interface`
      // `errors.dart`): a user dismiss arrives as `FailureCode.Canceled`,
      // every provider-side failure as another `FailureCode` (`Failed`,
      // `Timeout`, `Unknown`).
      if (e.error.code == FailureCode.Canceled) {
        throw PaymentSheetCancelled();
      }
      throw PaymentSheetFailure(
        e.error.localizedMessage ?? e.error.message ?? 'Plaćanje nije dovršeno',
      );
    }
  }
}
