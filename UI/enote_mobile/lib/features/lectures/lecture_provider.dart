import 'package:enote_core/enote_core.dart';

class LectureProvider extends ReadOnlyProvider<LectureDto> {
  LectureProvider({required super.apiClient})
    : super(endpoint: 'student/lectures');

  @override
  LectureDto fromJson(Map<String, dynamic> json) => LectureDto.fromJson(json);

  Future<RsvpResponse> rsvp(int id, RsvpRequest request) async {
    final response = await apiClient.post(
      '$endpoint/$id/rsvp',
      body: request.toJson(),
    );
    return RsvpResponse.fromJson(decodeOrThrow(response));
  }
}
