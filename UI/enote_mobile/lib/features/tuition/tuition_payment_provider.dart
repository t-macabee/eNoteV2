import 'package:enote_core/enote_core.dart';

import '../payments/payment_polling.dart' as polling;

/// Tuition payment state (`student/enrollments/{enrollmentId}/tuition`).
///
/// One intent per enrollment: the server reuses a `RequiresAction` intent for
/// 30 minutes and keys the Stripe request on the unpaid window, so every retry
/// calls `createIntent` again and never caches a `clientSecret`.
class TuitionPaymentProvider {
  final ApiClient apiClient;

  TuitionPaymentProvider({required this.apiClient});

  /// `GET student/enrollments/{enrollmentId}/tuition`.
  ///
  /// A 404 means the student has not started paying yet, which is a normal
  /// state and not an error.
  Future<CoursePaymentDto?> status(int enrollmentId) async {
    final response = await apiClient.get(
      'student/enrollments/$enrollmentId/tuition',
    );
    if (response.statusCode == 404) return null;
    return CoursePaymentDto.fromJson(decodeOrThrow(response));
  }

  /// `POST student/enrollments/{enrollmentId}/tuition/create-intent`.
  Future<CreateTuitionIntentResponse> createIntent(int enrollmentId) async {
    final response = await apiClient.post(
      'student/enrollments/$enrollmentId/tuition/create-intent',
    );
    return CreateTuitionIntentResponse.fromJson(decodeOrThrow(response));
  }

  /// `GET student/enrollments/{enrollmentId}/tuition/history`.
  Future<List<CoursePaymentDto>> history(int enrollmentId) async {
    final response = await apiClient.get(
      'student/enrollments/$enrollmentId/tuition/history',
    );
    return decodeList(response, CoursePaymentDto.fromJson);
  }

  /// Delegates to [polling.pollUntilSucceeded].
  Future<CoursePaymentDto?> pollUntilSucceeded(int enrollmentId) =>
      polling.pollUntilSucceeded(() => status(enrollmentId), (p) => p.status);
}
