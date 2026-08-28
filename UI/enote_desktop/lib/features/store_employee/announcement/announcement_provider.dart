import 'dart:convert';

import 'package:enote_core/enote_core.dart';

class StoreAnnouncementProvider extends BaseProvider<AnnouncementDto> {
  StoreAnnouncementProvider({required super.apiClient})
      : super(endpoint: 'shop/announcements');

  @override
  AnnouncementDto fromJson(Map<String, dynamic> json) =>
      AnnouncementDto.fromJson(json);

  Future<AnnouncementDto> uploadImage(
    int announcementId,
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    final response = await apiClient.postMultipart(
      '$endpoint/$announcementId/image',
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
