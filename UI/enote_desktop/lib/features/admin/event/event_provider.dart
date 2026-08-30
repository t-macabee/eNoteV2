import 'package:enote_core/enote_core.dart';

class EventProvider extends BaseProvider<EventDto> {
  EventProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/events');

  @override
  EventDto fromJson(Map<String, dynamic> json) => EventDto.fromJson(json);
}
