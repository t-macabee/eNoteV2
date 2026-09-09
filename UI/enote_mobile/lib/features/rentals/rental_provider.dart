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
      InstrumentRentalDto.fromJson(normalizeRentalStatus(json));

  /// Rewrites a string `rentalStatus` to the int the DTO understands.
  ///
  /// `Observed fact` (verified against the running API 2026-09-09): the
  /// backend serialises the enum as a name — `"rentalStatus": "Completed"` —
  /// while core `InstrumentRentalDto._parseStatus` accepts an `int` only and
  /// falls back to `pending` for anything else, so every rental would render
  /// as *Na čekanju*. Core is closed to changes in this workflow, so the
  /// provider normalises on the way in; the proper fix is an
  /// `InstrumentRentalStatus.fromDynamic` in core, next to the one
  /// `PaymentStatus` and `AnnouncementScope` already have.
  static Map<String, dynamic> normalizeRentalStatus(Map<String, dynamic> json) {
    final raw = json['rentalStatus'];
    if (raw is! String) return json;
    final match = InstrumentRentalStatus.values
        .where((s) => s.name.toLowerCase() == raw.toLowerCase())
        .firstOrNull;
    if (match == null) return json;
    return {...json, 'rentalStatus': match.toJson()};
  }

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
