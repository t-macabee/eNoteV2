import 'package:enote_core/enote_core.dart';

/// Student events over `student/events` (01 §4.3, §6.1, gap A2 closed).
class EventProvider extends ReadOnlyProvider<EventDto> {
  EventProvider({required super.apiClient}) : super(endpoint: 'student/events');

  @override
  EventDto fromJson(Map<String, dynamic> json) => EventDto.fromJson(json);
}
