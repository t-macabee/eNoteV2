import 'package:enote_core/enote_core.dart';

class EventProvider extends ReadOnlyProvider<EventDto> {
  EventProvider({required super.apiClient}) : super(endpoint: 'student/events');

  @override
  EventDto fromJson(Map<String, dynamic> json) => EventDto.fromJson(json);
}
