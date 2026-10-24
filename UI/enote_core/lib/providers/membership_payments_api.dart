import '../api/api_response.dart';
import '../paging/paged_result.dart';
import '../providers/base_provider.dart';
import '../models/identity/membership_models.dart';
import '../paging/paged_query.dart';

mixin MembershipPaymentsApi<T> on ReadOnlyProvider<T> {
  Future<PagedResult<MembershipPaymentDto>> getMembershipPayments(
    int studentId, {
    int page = 1,
    int pageSize = 10,
  }) async {
    final response = await apiClient.get(
      '$endpoint/$studentId/membership-payments',
      queryParams: pagedQuery(page, pageSize, ''),
    );

    return parsePage<MembershipPaymentDto>(
      response,
      MembershipPaymentDto.fromJson,
    );
  }

  Future<MembershipPaymentDto> recordMembershipPayment(
    int studentId,
    RecordMembershipPaymentRequest request,
  ) async {
    final response = await apiClient.post(
      '$endpoint/$studentId/membership-payments',
      body: request.toJson(),
    );
    final data = decodeOrThrow(response);
    notifyListeners();
    return MembershipPaymentDto.fromJson(data);
  }

  Future<void> sendMembershipWarning(int studentId, {String? message}) async {
    final response = await apiClient.post(
      '$endpoint/$studentId/membership-warning',
      body: SendMembershipWarningRequest(message: message).toJson(),
    );
    throwIfError(response);
  }
}
