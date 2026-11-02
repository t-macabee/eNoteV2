import 'package:enote_core/enote_core.dart';

enum LectureFilter {
  upcoming,
  all;

  Map<String, dynamic> query(DateTime now) => switch (this) {
    LectureFilter.upcoming => {'from': toUtcIso(now)},
    LectureFilter.all => const <String, dynamic>{},
  };
}

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
    final result = RsvpResponse.fromJson(decodeOrThrow(response));
    notifyListeners();
    return result;
  }

  Future<int?> countUpcoming() async {
    final result = await getPage(
      params: pagedQuery(
        1,
        1,
        '',
        filters: LectureFilter.upcoming.query(DateTime.now()),
      ),
    );
    return result.totalCount;
  }
}
