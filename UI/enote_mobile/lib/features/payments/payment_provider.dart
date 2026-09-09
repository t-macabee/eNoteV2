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
}
