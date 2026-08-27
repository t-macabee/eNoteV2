import 'dart:convert';

import 'package:enote_core/enote_core.dart';

class InstrumentProvider extends BaseProvider<InstrumentDto> {
  InstrumentProvider({required super.apiClient})
      : super(endpoint: 'shop/instruments');

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);

  Future<InstrumentDto> uploadImage(
    int instrumentId,
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    final response = await apiClient.postMultipart(
      '$endpoint/$instrumentId/image',
      bytes: bytes,
      fileName: fileName,
      contentType: contentType,
    );
    if (response.statusCode >= 400) {
      throw ApiException(
        ApiErrorMapper.mapError(response.statusCode, response.body),
      );
    }
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final updated = fromJson(data);
    notifyListeners();
    return updated;
  }
}
