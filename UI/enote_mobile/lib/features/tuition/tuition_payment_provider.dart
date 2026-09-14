import 'dart:convert';

import 'package:enote_core/enote_core.dart';

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
    throwIfError(response);
    final decoded = jsonDecode(response.body);
    if (decoded is! List) return const [];
    return decoded
        .map(
          (item) => CoursePaymentDto.fromJson(
            Map<String, dynamic>.from(item as Map),
          ),
        )
        .toList();
  }

  /// Polls [status] up to 5 × 2 s until it reports `succeeded`.
  ///
  /// Returns the succeeded payment, or the last observation (still pending or
  /// `null` when no payment row exists yet) so the caller can show state F
  /// instead of a false success.
  Future<CoursePaymentDto?> pollUntilSucceeded(int enrollmentId) async {
    CoursePaymentDto? last;
    for (var i = 0; i < 5; i++) {
      await Future.delayed(const Duration(seconds: 2));
      last = await status(enrollmentId);
      if (last?.status == PaymentStatus.succeeded) return last;
    }
    return last;
  }
}
