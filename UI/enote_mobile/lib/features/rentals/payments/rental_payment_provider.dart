import 'package:enote_core/enote_core.dart';

import '../../payments/payment_polling.dart' as polling;

class RentalPaymentProvider {
  final ApiClient apiClient;

  RentalPaymentProvider({required this.apiClient});

  Future<RentalPaymentDto?> status(int rentalId) async {
    final response = await apiClient.get('student/rentals/$rentalId/payments');
    if (response.statusCode == 404) return null;
    return RentalPaymentDto.fromJson(decodeOrThrow(response));
  }

  Future<CreatePaymentIntentResponse> createIntent(int rentalId) async {
    final response = await apiClient.post(
      'student/rentals/$rentalId/payments/create-intent',
    );
    return CreatePaymentIntentResponse.fromJson(decodeOrThrow(response));
  }

  Future<RentalPaymentDto?> pollUntilSucceeded(int rentalId) =>
      polling.pollUntilSucceeded(() => status(rentalId), (p) => p.status);
}
