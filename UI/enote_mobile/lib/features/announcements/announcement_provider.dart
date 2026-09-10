import 'package:enote_core/enote_core.dart';

/// Student announcement feed over `student/announcements` (01 §4.3, §6.1).
class AnnouncementProvider extends ReadOnlyProvider<AnnouncementDto> {
  AnnouncementProvider({required super.apiClient})
    : super(endpoint: 'student/announcements');

  @override
  AnnouncementDto fromJson(Map<String, dynamic> json) =>
      AnnouncementDto.fromJson(json);
}
