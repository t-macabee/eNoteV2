import 'package:enote_core/enote_core.dart';

class StoreAnnouncementProvider extends BaseProvider<AnnouncementDto> {
  StoreAnnouncementProvider({required super.apiClient})
      : super(endpoint: 'shop/announcements');

  @override
  AnnouncementDto fromJson(Map<String, dynamic> json) =>
      AnnouncementDto.fromJson(json);
}
