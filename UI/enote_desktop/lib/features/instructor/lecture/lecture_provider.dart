import 'package:enote_core/enote_core.dart';

class LectureProvider extends BaseProvider<LectureDto> {
  LectureProvider({required super.apiClient}) : super(endpoint: 'instructor/lectures');

  @override
  LectureDto fromJson(Map<String, dynamic> json) => LectureDto.fromJson(json);

  Future<LectureDto> cancel(int id) async {
    final response = await apiClient.post('$endpoint/$id/cancel');
    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }

  Future<PagedResult<AttendanceDto>> getAttendance(
    int lectureId, {
    Map<String, dynamic>? params,
  }) async {
    final response = await apiClient.get(
      '$endpoint/$lectureId/attendance',
      queryParams: params,
    );
    throwIfError(response);
    return parsePage<AttendanceDto>(
      response,
      (json) => AttendanceDto.fromJson(json),
      params: params,
    );
  }

  Future<AttendanceDto> markAttendance(
    int lectureId,
    MarkAttendanceRequest request,
  ) async {
    final response = await apiClient.put(
      '$endpoint/$lectureId/attendance',
      body: request.toJson(),
    );
    final data = decodeOrThrow(response);
    return AttendanceDto.fromJson(data);
  }
}
