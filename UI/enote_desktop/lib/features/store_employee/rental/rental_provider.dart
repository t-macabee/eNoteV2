import 'package:enote_core/enote_core.dart';

class RentalProvider extends CrudProvider<InstrumentRentalDto> {
  RentalProvider({required super.apiClient}) : super(endpoint: 'shop/rentals');

  @override
  InstrumentRentalDto fromJson(Map<String, dynamic> json) =>
      InstrumentRentalDto.fromJson(json);

  Future<int?> countPending({int? instrumentId}) async {
    final page = await getPage(
      params: pagedQuery(
        1,
        1,
        '',
        filters: {
          'rentalStatus': InstrumentRentalStatus.pending.toJson(),
          'instrumentId': ?instrumentId,
        },
      ),
    );
    return page.totalCount;
  }

  Future<InstrumentRentalDto> transition(
    int id,
    RentalTrigger trigger, {
    String? note,
  }) async {
    final response = await apiClient.post(
      '$endpoint/$id/${_transitionPath(trigger)}',
      body: (note == null || note.trim().isEmpty)
          ? null
          : RentalStatusRequest(note: note).toJson(),
    );

    final data = decodeOrThrow(response);
    final updated = fromJson(data);
    notifyListeners();
    return updated;
  }

  Future<RentalPaymentDto> refund(int rentalId, {int? amountCents}) async {
    final response = await apiClient.post(
      '$endpoint/$rentalId/payments/refund',
      body: RefundRequest(amountCents: amountCents).toJson(),
    );

    final data = decodeOrThrow(response);
    notifyListeners();
    return RentalPaymentDto.fromJson(data);
  }

  Future<RentalPaymentDto?> getPaymentStatus(int rentalId) async {
    final response = await apiClient.get('$endpoint/$rentalId/payments');

    if (response.statusCode == 404) {
      return null;
    }

    final data = decodeOrThrow(response);
    return RentalPaymentDto.fromJson(data);
  }

  static String _transitionPath(RentalTrigger trigger) => switch (trigger) {
    RentalTrigger.approve => 'approve',
    RentalTrigger.reject => 'reject',
    RentalTrigger.pickup => 'pickup',
    RentalTrigger.complete => 'complete',
    RentalTrigger.returnEarly => 'return-early',
    RentalTrigger.cancel => 'cancel',
  };
}
