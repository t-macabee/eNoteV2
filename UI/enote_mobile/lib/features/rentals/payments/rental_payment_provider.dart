import 'package:enote_core/enote_core.dart';

import '../../payments/payment_polling.dart' as polling;

/// Rental payment state (`student/rentals/{rentalId}/payments`).
///
/// Phase 5 needs the read side only: S11 shows what has been paid or
/// refunded. `createIntent` and the status poll arrive with the payment
/// screen (T48).
class RentalPaymentProvider {
  final ApiClient apiClient;

  RentalPaymentProvider({required this.apiClient});

  /// `GET student/rentals/{rentalId}/payments`.
  ///
  /// A 404 means the student has not started paying yet, which is a normal
  /// state and not an error (01 §6.5).
  Future<RentalPaymentDto?> status(int rentalId) async {
    final response = await apiClient.get('student/rentals/$rentalId/payments');
    if (response.statusCode == 404) return null;
    return RentalPaymentDto.fromJson(decodeOrThrow(response));
  }

  /// `POST student/rentals/{rentalId}/payments/create-intent`.
  ///
  /// The `clientSecret` is never cached: every retry calls this again and
  /// the server reuses the same `RequiresAction` intent within 30 minutes
  /// (02 §10.4).
  Future<CreatePaymentIntentResponse> createIntent(int rentalId) async {
    final response = await apiClient.post(
      'student/rentals/$rentalId/payments/create-intent',
    );
    return CreatePaymentIntentResponse.fromJson(decodeOrThrow(response));
  }

  /// Delegates to [polling.pollUntilSucceeded].
  Future<RentalPaymentDto?> pollUntilSucceeded(int rentalId) =>
      polling.pollUntilSucceeded(() => status(rentalId), (p) => p.status);
}
