import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/users/admin_user_provider.dart';

import 'helpers.dart';

ScriptedClient _mockClient({int statusCode = 200, String responseBody = '{}'}) =>
    ScriptedClient((_) => jsonResponse(responseBody, statusCode));

AdminUserProvider _buildProvider(ScriptedClient mockClient) {
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: mockClient,
  );
  return AdminUserProvider(apiClient: apiClient);
}

void main() {
  group('getProfile', () {
    test('200 decodes into UserProfileResponse', () async {
      final mockClient = _mockClient(
        statusCode: 200,
        responseBody: jsonEncode({
          'role': 'Student',
          'username': 'lbeethoven',
          'email': 'l@example.com',
          'profile': {
            'id': 22,
            'firstName': 'Ludwig',
            'lastName': 'Beethoven',
          },
        }),
      );
      final provider = _buildProvider(mockClient);

      final profile = await provider.getProfile(22);

      expect(profile.role, 'Student');
      expect(profile.profile.firstName, 'Ludwig');
      expect(mockClient.recorded.single.method, 'GET');
      expect(mockClient.recorded.single.url, endsWith('admin/users/22'));
    });

    test('400 throws ApiException with mapped message', () async {
      final mockClient = _mockClient(
        statusCode: 400,
        responseBody: jsonEncode({'message': 'Korisnik nije pronađen.'}),
      );
      final provider = _buildProvider(mockClient);

      expect(
        () => provider.getProfile(999),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Korisnik nije pronađen.',
          ),
        ),
      );
    });
  });

  group('setStatus', () {
    test('sends PUT with isActive body and succeeds on 204', () async {
      final mockClient = _mockClient(statusCode: 204, responseBody: '');
      final provider = _buildProvider(mockClient);

      await provider.setStatus(44, false);

      expect(mockClient.recorded.single.method, 'PUT');
      expect(mockClient.recorded.single.url, endsWith('admin/users/44/status'));
      expect(mockClient.recorded.single.body, {'isActive': false});
    });

    test('400 throws ApiException with mapped message', () async {
      final mockClient = _mockClient(
        statusCode: 400,
        responseBody: jsonEncode({'message': 'Nevažeći status.'}),
      );
      final provider = _buildProvider(mockClient);

      expect(
        () => provider.setStatus(44, true),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Nevažeći status.',
          ),
        ),
      );
    });
  });

  group('renewMembership', () {
    test('sends PUT with paidUntil body and returns the picked date on success', () async {
      final mockClient = _mockClient(statusCode: 204, responseBody: '');
      final provider = _buildProvider(mockClient);
      final paidUntil = DateTime(2027, 3, 1);

      final result = await provider.renewMembership(22, paidUntil);

      expect(result, paidUntil);
      expect(mockClient.recorded.single.method, 'PUT');
      expect(mockClient.recorded.single.url, endsWith('admin/users/22/membership'));
      expect(mockClient.recorded.single.body, {'paidUntil': paidUntil.toIso8601String()});
    });

    test('400 throws ApiException with mapped message', () async {
      final mockClient = _mockClient(
        statusCode: 400,
        responseBody: jsonEncode({'message': 'Nevažeći datum.'}),
      );
      final provider = _buildProvider(mockClient);

      expect(
        () => provider.renewMembership(22, DateTime(2027, 1, 1)),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Nevažeći datum.',
          ),
        ),
      );
    });
  });
}
