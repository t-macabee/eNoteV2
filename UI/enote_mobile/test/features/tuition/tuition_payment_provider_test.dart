import 'dart:convert';

import 'package:fake_async/fake_async.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/tuition/tuition_payment_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';
const _enrollmentId = 4;

const _intent = {
  'enrollmentId': _enrollmentId,
  'paymentIntentId': 'pi_tuition_123',
  'clientSecret': 'pi_tuition_123_secret_abc',
  'amountCents': 80000,
  'currency': 'bam',
  'status': 'RequiresAction',
};

Map<String, dynamic> _payment(String status) => {
  'id': 9,
  'enrollmentId': _enrollmentId,
  'paymentIntentId': 'pi_tuition_123',
  'amountCents': 80000,
  'currency': 'bam',
  'status': status,
  'paidAt': '2026-09-09T12:41:00',
  'periodStart': '2026-09-09T12:41:00',
  'periodEnd': '2026-10-09T12:41:00',
};

/// Serves one queued response per request so poll sequences can be scripted.
class _ScriptedClient extends http.BaseClient {
  final List<http.BaseRequest> requests = [];
  final List<({int statusCode, Object body})> script;
  final ({int statusCode, Object body}) fallback;

  _ScriptedClient(this.script, {Object? fallbackBody})
    : fallback = (
        statusCode: 200,
        body: fallbackBody ?? {'status': 'RequiresAction'},
      );

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request);
    final next = script.isNotEmpty
        ? script.removeAt(0)
        : (statusCode: fallback.statusCode, body: fallback.body);
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(next.body))),
      next.statusCode,
      headers: {'content-type': 'application/json'},
    );
  }
}

TuitionPaymentProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return TuitionPaymentProvider(
    apiClient: ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: client,
    ),
  );
}

int _statusCalls(_ScriptedClient client) => client.requests
    .where(
      (r) =>
          r.method == 'GET' &&
          (r as http.Request).url.path.endsWith(
            '/student/enrollments/$_enrollmentId/tuition',
          ),
    )
    .length;

void main() {
  test('createIntent posts to create-intent and decodes the response', () async {
    final client = _ScriptedClient([(statusCode: 200, body: _intent)]);
    final provider = _provider(client);
    final intent = await provider.createIntent(_enrollmentId);

    final sent = client.requests.single as http.Request;
    expect(sent.method, 'POST');
    expect(
      sent.url.path,
      '/api/v1/student/enrollments/$_enrollmentId/tuition/create-intent',
    );
    expect(intent.paymentIntentId, 'pi_tuition_123');
    expect(intent.clientSecret, 'pi_tuition_123_secret_abc');
    expect(intent.amountCents, 80000);
    expect(intent.currency, 'bam');
    expect(intent.status, PaymentStatus.requiresAction);
  });

  test('status returns the payment and 404 means not started', () async {
    final client = _ScriptedClient([
      (statusCode: 200, body: _payment('Succeeded')),
    ]);
    final latest = await _provider(client).status(_enrollmentId);
    expect(latest?.status, PaymentStatus.succeeded);
    expect(latest?.periodEnd, DateTime.parse('2026-10-09T12:41:00'));

    final missing = _ScriptedClient([(statusCode: 404, body: {})]);
    expect(await _provider(missing).status(_enrollmentId), isNull);
  });

  test('history decodes a list', () async {
    final client = _ScriptedClient([], fallbackBody: [_payment('Succeeded')]);
    final history = await _provider(client).history(_enrollmentId);
    expect(history, hasLength(1));
    expect(history.single.status, PaymentStatus.succeeded);
  });

  test('poll succeeds on poll 3 after exactly 3 status calls', () {
    fakeAsync((async) {
      final client = _ScriptedClient([
        (statusCode: 200, body: _payment('RequiresAction')),
        (statusCode: 200, body: _payment('RequiresAction')),
        (statusCode: 200, body: _payment('Succeeded')),
      ]);
      final provider = _provider(client);

      CoursePaymentDto? result;
      provider.pollUntilSucceeded(_enrollmentId).then((value) => result = value);
      async.elapse(const Duration(seconds: 7));

      expect(result?.status, PaymentStatus.succeeded);
      expect(_statusCalls(client), 3);
    });
  });

  test('poll still pending after 5 requiresAction observations', () {
    fakeAsync((async) {
      final client = _ScriptedClient([]);
      final provider = _provider(client);

      CoursePaymentDto? result;
      var done = false;
      provider.pollUntilSucceeded(_enrollmentId).then((value) {
        result = value;
        done = true;
      });
      async.elapse(const Duration(seconds: 11));

      expect(done, isTrue);
      expect(result?.status, PaymentStatus.requiresAction);
      expect(_statusCalls(client), 5);
    });
  });

  test('a retry calls create-intent again, never reusing a secret', () async {
    final client = _ScriptedClient([
      (statusCode: 200, body: _intent),
      (statusCode: 200, body: _intent),
    ]);
    final provider = _provider(client);

    await provider.createIntent(_enrollmentId);
    await provider.createIntent(_enrollmentId);

    final creates = client.requests.where(
      (r) =>
          r.method == 'POST' &&
          (r as http.Request).url.path.endsWith('/tuition/create-intent'),
    );
    expect(creates, hasLength(2));
  });
}
