import 'dart:convert';

import 'package:enote_core/enote_core.dart';

class LectureProvider extends BaseProvider<LectureDto> {
  LectureProvider({required super.apiClient}) : super(endpoint: 'instructor/lectures');

  @override
  LectureDto fromJson(Map<String, dynamic> json) => LectureDto.fromJson(json);

  Future<LectureDto> cancel(int id) async {
    final response = await apiClient.post('$endpoint/$id/cancel');
    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }
    final data = jsonDecode(response.body) as Map<String, dynamic>;
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
    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }
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
    if (response.statusCode >= 400) {
      throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
    }
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return AttendanceDto.fromJson(data);
  }
}
