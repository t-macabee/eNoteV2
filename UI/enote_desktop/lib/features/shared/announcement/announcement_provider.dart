import 'package:enote_core/enote_core.dart';

class AnnouncementProvider extends CrudProvider<AnnouncementDto> {
  AnnouncementProvider({required super.apiClient, required int courseId})
      : super(endpoint: 'instructor/courses/$courseId/announcements');

  @override
  AnnouncementDto fromJson(Map<String, dynamic> json) =>
      AnnouncementDto.fromJson(json);
}
