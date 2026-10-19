import 'package:enote_core/enote_core.dart';

class AnnouncementProvider extends ReadOnlyProvider<AnnouncementDto> {
  AnnouncementProvider({required super.apiClient})
    : super(endpoint: 'student/announcements');

  @override
  AnnouncementDto fromJson(Map<String, dynamic> json) =>
      AnnouncementDto.fromJson(json);
}
