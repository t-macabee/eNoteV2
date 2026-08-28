import 'dart:convert';

import 'package:enote_core/enote_core.dart';

class RentalProvider extends BaseProvider<InstrumentRentalDto> {
  RentalProvider({required super.apiClient}) : super(endpoint: 'shop/rentals');

  @override
  InstrumentRentalDto fromJson(Map<String, dynamic> json) =>
      InstrumentRentalDto.fromJson(json);

  Future<InstrumentRentalDto> approve(int id, {String? note}) =>
      _transition(id, 'approve', note);

  Future<InstrumentRentalDto> reject(int id, {String? note}) =>
      _transition(id, 'reject', note);

  Future<InstrumentRentalDto> pickup(int id, {String? note}) =>
      _transition(id, 'pickup', note);

  Future<InstrumentRentalDto> complete(int id, {String? note}) =>
      _transition(id, 'complete', note);

  Future<InstrumentRentalDto> returnEarly(int id, {String? note}) =>
      _transition(id, 'return-early', note);

  Future<RentalPaymentDto> refund(int rentalId, {int? amountCents}) async {
    final response = await apiClient.post(
      '$endpoint/$rentalId/payments/refund',
      body: RefundRequest(amountCents: amountCents).toJson(),
    );

    if (response.statusCode >= 400) {
      throw ApiException(
          ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    notifyListeners();
    return RentalPaymentDto.fromJson(data);
  }

  Future<RentalPaymentDto?> getPaymentStatus(int rentalId) async {
    final response = await apiClient.get('$endpoint/$rentalId/payments');

    if (response.statusCode == 404) {
      return null;
    }
    if (response.statusCode >= 400) {
      throw ApiException(
          ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return RentalPaymentDto.fromJson(data);
  }

  Future<InstrumentRentalDto> _transition(
      int id, String action, String? note) async {
    final response = await apiClient.post('$endpoint/$id/$action',
        body: (note == null || note.trim().isEmpty)
            ? null
            : RentalStatusRequest(note: note).toJson());

    if (response.statusCode >= 400) {
      throw ApiException(
          ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final updated = fromJson(data);
    notifyListeners();
    return updated;
  }
}
