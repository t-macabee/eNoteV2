import 'package:enote_core/enote_core.dart';

import '../payments/payment_polling.dart' as polling;

class TuitionPaymentProvider {
  final ApiClient apiClient;

  TuitionPaymentProvider({required this.apiClient});

  Future<CoursePaymentDto?> status(int enrollmentId) async {
    final response = await apiClient.get(
      'student/enrollments/$enrollmentId/tuition',
    );
    if (response.statusCode == 404) return null;
    return CoursePaymentDto.fromJson(decodeOrThrow(response));
  }

  Future<CreateTuitionIntentResponse> createIntent(int enrollmentId) async {
    final response = await apiClient.post(
      'student/enrollments/$enrollmentId/tuition/create-intent',
    );
    return CreateTuitionIntentResponse.fromJson(decodeOrThrow(response));
  }

  Future<List<CoursePaymentDto>> history(int enrollmentId) async {
    final response = await apiClient.get(
      'student/enrollments/$enrollmentId/tuition/history',
    );
    return decodeList(response, CoursePaymentDto.fromJson);
  }

  Future<CoursePaymentDto?> pollUntilSucceeded(int enrollmentId) =>
      polling.pollUntilSucceeded(() => status(enrollmentId), (p) => p.status);
}
