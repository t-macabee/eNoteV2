import 'package:enote_core/enote_core.dart';

class StoreInstrumentProvider extends ReadOnlyProvider<InstrumentDto> {
  StoreInstrumentProvider({
    required super.apiClient,
  }) : super(endpoint: 'instruments/public');

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);

  Future<PagedResult<InstrumentDto>> fetchPage(
    int page,
    int pageSize,
    String search, {
    required int musicStoreId,
    int? instrumentTypeId,
  }) {
    return getPage(
      params: {
        'search.page': page,
        'search.pageSize': pageSize,
        'search.includeTotalCount': true,
        'search.musicStoreId': musicStoreId,
        if (search.isNotEmpty) 'search.search': search,
        'search.instrumentTypeId': ?instrumentTypeId,
      },
    );
  }
}
