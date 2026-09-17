import 'package:enote_core/enote_core.dart';

/// Student lectures over `student/lectures` (01 §4.3).
class LectureProvider extends ReadOnlyProvider<LectureDto> {
  LectureProvider({required super.apiClient})
    : super(endpoint: 'student/lectures');

  @override
  LectureDto fromJson(Map<String, dynamic> json) => LectureDto.fromJson(json);

  /// POST `student/lectures/{id}/rsvp` with `{confirm}`.
  Future<RsvpResponse> rsvp(int id, RsvpRequest request) async {
    final response = await apiClient.post(
      '$endpoint/$id/rsvp',
      body: request.toJson(),
    );
    return RsvpResponse.fromJson(decodeOrThrow(response));
  }
}
