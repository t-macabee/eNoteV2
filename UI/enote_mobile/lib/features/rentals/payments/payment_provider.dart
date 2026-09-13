import 'package:enote_core/enote_core.dart';

/// Rental payment state (`student/rentals/{rentalId}/payments`).
///
/// Phase 5 needs the read side only: S11 shows what has been paid or
/// refunded. `createIntent` and the status poll arrive with the payment
/// screen (T48).
class PaymentProvider {
  final ApiClient apiClient;

  PaymentProvider({required this.apiClient});

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

  /// Polls [status] up to 5 × 2 s until it reports `succeeded` (01 §6.4).
  ///
  /// Returns the succeeded payment, or the last observation (still pending
  /// or `null` when no payment row exists yet) so the caller can show
  /// state F instead of a false success.
  Future<RentalPaymentDto?> pollUntilSucceeded(int rentalId) async {
    RentalPaymentDto? last;
    for (var i = 0; i < 5; i++) {
      await Future.delayed(const Duration(seconds: 2));
      last = await status(rentalId);
      if (last?.status == PaymentStatus.succeeded) return last;
    }
    return last;
  }
}
