import 'package:enote_core/enote_core.dart';

/// The student's own rentals (S10, S11) plus the two composed checks the
/// catalogue and the request sheet need: the unpaid-debt gate (01 §6.4) and
/// the create/cancel commands.
class RentalProvider extends ReadOnlyProvider<InstrumentRentalDto> {
  /// Rows per rental page (02 §3).
  static const int pageSize = 20;

  RentalProvider({required super.apiClient}) : super(endpoint: 'student/rentals');

  @override
  InstrumentRentalDto fromJson(Map<String, dynamic> json) =>
      InstrumentRentalDto.fromJson(json);

  /// One page of the student's rentals. The server has no text search here,
  /// so the only filters are the status dropdown and an optional instrument.
  Future<PagedResult<InstrumentRentalDto>> fetchPage(
    int page,
    int pageSize, {
    InstrumentRentalStatus? rentalStatus,
    int? instrumentId,
  }) {
    return getPage(
      params: pagedQuery(
        page,
        pageSize,
        '',
        filters: {
          if (rentalStatus != null) 'rentalStatus': rentalStatus.toJson(),
          'instrumentId': ?instrumentId,
        },
      ),
    );
  }

  /// `POST student/rentals` → 201 with the created rental.
  Future<InstrumentRentalDto> createRequest(RentalCreateRequest request) async {
    final response = await apiClient.post(
      'student/rentals',
      body: request.toJson(),
    );
    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }

  /// `POST student/rentals/{id}/cancel`.
  Future<void> cancel(int id, {String? note}) async {
    final response = await apiClient.post(
      'student/rentals/$id/cancel',
      body: RentalStatusRequest(note: note).toJson(),
    );
    throwIfError(response);
    notifyListeners();
  }

  /// `GET student/rentals/debt` → the earliest unpaid completed rental, if
  /// any. The server stays authoritative: its refusal message is what the
  /// request sheet shows when the two disagree.
  Future<RentalDebtDto> debt() async {
    final response = await apiClient.get('student/rentals/debt');
    return RentalDebtDto.fromJson(decodeOrThrow(response));
  }
}
