import 'package:enote_core/enote_core.dart';

class RentalProvider extends ReadOnlyProvider<InstrumentRentalDto> {
  static const int pageSize = 20;

  RentalProvider({required super.apiClient}) : super(endpoint: 'student/rentals');

  @override
  InstrumentRentalDto fromJson(Map<String, dynamic> json) =>
      InstrumentRentalDto.fromJson(json);

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

  Future<InstrumentRentalDto> createRequest(RentalCreateRequest request) async {
    final response = await apiClient.post(
      'student/rentals',
      body: request.toJson(),
    );
    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }

  Future<void> cancel(int id, {String? note}) async {
    final response = await apiClient.post(
      'student/rentals/$id/cancel',
      body: RentalStatusRequest(note: note).toJson(),
    );
    throwIfError(response);
    notifyListeners();
  }

  Future<RentalDebtDto> debt() async {
    final response = await apiClient.get('student/rentals/debt');
    return RentalDebtDto.fromJson(decodeOrThrow(response));
  }
}
