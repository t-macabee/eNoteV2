import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/student/student_provider.dart';
import 'package:enote_desktop/features/instructor/student/instructor_student_provider.dart';

import 'helpers.dart';

ScriptedClient _mockClient({
  int statusCode = 200,
  String responseBody = '{}',
}) =>
    ScriptedClient((_) => jsonResponse(responseBody, statusCode));

ApiClient _buildApiClient(ScriptedClient mockClient) => ApiClient(
  baseUrl: 'http://localhost:5059/api/v1/',
  authState: AuthState(),
  httpClient: mockClient,
);

Map<String, dynamic> _paymentJson() => {
  'id': 7,
  'studentId': 5,
  'amount': 40.0,
  'paidAt': '2026-09-01T00:00:00Z',
  'periodStart': '2026-09-01T00:00:00Z',
  'periodEnd': '2027-01-01T00:00:00Z',
  'receiptNumber': 'CL-7',
  'note': 'Gotovina',
};

void main() {
  group('getMembershipPayments', () {
    test('requests the paged payments of the admin students endpoint',
        () async {
      final mockClient = _mockClient(
        responseBody: jsonEncode({
          'items': [_paymentJson()],
          'page': 1,
          'pageSize': 5,
          'totalCount': 1,
        }),
      );
      final provider = StudentProvider(apiClient: _buildApiClient(mockClient));

      final result = await provider.getMembershipPayments(5, pageSize: 5);

      expect(mockClient.recorded.single.method, 'GET');
      expect(
        mockClient.recorded.single.url,
        endsWith(
          'admin/students/5/membership-payments?page=1&pageSize=5&includeTotalCount=true',
        ),
      );
      final payment = result.items.single;
      expect(payment.receiptNumber, 'CL-7');
      expect(payment.amount, 40.0);
      expect(payment.note, 'Gotovina');
    });

    test('uses the instructor students endpoint for the instructor provider',
        () async {
      final mockClient = _mockClient(
        responseBody: jsonEncode({
          'items': [],
          'page': 1,
          'pageSize': 5,
          'totalCount': 0,
        }),
      );
      final provider = InstructorStudentProvider(
        apiClient: _buildApiClient(mockClient),
      );

      await provider.getMembershipPayments(5);

      expect(
        mockClient.recorded.single.url,
        contains('instructor/students/5/membership-payments'),
      );
    });
  });

  group('recordMembershipPayment', () {
    test('posts the receipt and returns the recorded payment', () async {
      final mockClient = _mockClient(
        statusCode: 201,
        responseBody: jsonEncode(_paymentJson()),
      );
      final provider = StudentProvider(apiClient: _buildApiClient(mockClient));

      final payment = await provider.recordMembershipPayment(
        5,
        RecordMembershipPaymentRequest(
          amount: 40,
          periodEnd: DateTime.utc(2027, 1, 1),
          receiptNumber: 'CL-7',
          note: 'Gotovina',
        ),
      );

      expect(payment.receiptNumber, 'CL-7');
      expect(mockClient.recorded.single.method, 'POST');
      expect(
        mockClient.recorded.single.url,
        endsWith('admin/students/5/membership-payments'),
      );
      expect(mockClient.recorded.single.body, {
        'amount': 40.0,
        'periodEnd': DateTime.utc(2027, 1, 1).toIso8601String(),
        'receiptNumber': 'CL-7',
        'note': 'Gotovina',
      });
    });

    test('400 throws ApiException with the server message', () async {
      final mockClient = _mockClient(
        statusCode: 400,
        responseBody: jsonEncode({
          'message': 'Potvrda s ovim brojem je već evidentirana.',
        }),
      );
      final provider = StudentProvider(apiClient: _buildApiClient(mockClient));

      expect(
        () => provider.recordMembershipPayment(
          5,
          RecordMembershipPaymentRequest(amount: 40, months: 1, receiptNumber: 'CL-7'),
        ),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Potvrda s ovim brojem je već evidentirana.',
          ),
        ),
      );
    });
  });

  group('sendMembershipWarning', () {
    test('posts the warning message and succeeds on 204', () async {
      final mockClient = _mockClient(statusCode: 204, responseBody: '');
      final provider = StudentProvider(apiClient: _buildApiClient(mockClient));

      await provider.sendMembershipWarning(5, message: 'Obnovite članarinu.');

      expect(mockClient.recorded.single.method, 'POST');
      expect(
        mockClient.recorded.single.url,
        endsWith('admin/students/5/membership-warning'),
      );
      expect(mockClient.recorded.single.body, {
        'message': 'Obnovite članarinu.',
      });
    });

    test('400 throws ApiException with the server message', () async {
      final mockClient = _mockClient(
        statusCode: 400,
        responseBody: jsonEncode({
          'message': 'Upozorenje je već poslano u posljednja 24 sata.',
        }),
      );
      final provider = StudentProvider(apiClient: _buildApiClient(mockClient));

      expect(
        () => provider.sendMembershipWarning(5),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Upozorenje je već poslano u posljednja 24 sata.',
          ),
        ),
      );
    });
  });
}
