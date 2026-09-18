
import 'package:fake_async/fake_async.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/payments/rental_payment_provider.dart';

import '../../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

const _intent = {
  'rentalId': 2,
  'paymentIntentId': 'pi_test_123',
  'clientSecret': 'pi_test_123_secret_abc',
  'amountCents': 4500,
  'currency': 'bam',
  'status': 'RequiresAction',
};

Map<String, dynamic> _payment(String status) => {
  'id': 7,
  'rentalId': 2,
  'paymentIntentId': 'pi_test_123',
  'amountCents': 4500,
  'currency': 'bam',
  'status': status,
};

ScriptedClient _scriptedClient(
  List<({int statusCode, Map<String, dynamic> body})> script, {
  Map<String, dynamic>? fallbackBody,
}) {
  final fallback = (
    statusCode: 200,
    body: fallbackBody ?? {'status': 'RequiresAction'},
  );
  return ScriptedClient((_) {
    final next = script.isNotEmpty ? script.removeAt(0) : fallback;
    return jsonResponse(next.body, next.statusCode);
  });
}

RentalPaymentProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return RentalPaymentProvider(
    apiClient: ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: client,
    ),
  );
}

int _statusCalls(ScriptedClient client) => client.requests
    .where(
      (r) =>
          r.method == 'GET' &&
          (r).url.path.endsWith('/student/rentals/2/payments'),
    )
    .length;

void main() {
  test('createIntent posts to create-intent and decodes the response', () async {
    final client = _scriptedClient([(statusCode: 200, body: _intent)]);
    final provider = _provider(client);
    final intent = await provider.createIntent(2);

    final sent = client.requests.single;
    expect(sent.method, 'POST');
    expect(
      sent.url.path,
      '/api/v1/student/rentals/2/payments/create-intent',
    );
    expect(intent.paymentIntentId, 'pi_test_123');
    expect(intent.clientSecret, 'pi_test_123_secret_abc');
    expect(intent.amountCents, 4500);
    expect(intent.currency, 'bam');
    expect(intent.status, PaymentStatus.requiresAction);
  });

  test('status returns the payment and 404 means not started', () async {
    final client = _scriptedClient([(statusCode: 200, body: _payment('Succeeded'))]);
    expect(
      (await _provider(client).status(2))?.status,
      PaymentStatus.succeeded,
    );

    final missing = _scriptedClient([(statusCode: 404, body: {})]);
    expect(await _provider(missing).status(2), isNull);
  });

  test('poll succeeds on poll 3 after exactly 3 status calls', () {
    fakeAsync((async) {
      final client = _scriptedClient([
        (statusCode: 200, body: _payment('RequiresAction')),
        (statusCode: 200, body: _payment('RequiresAction')),
        (statusCode: 200, body: _payment('Succeeded')),
      ]);
      final provider = _provider(client);

      RentalPaymentDto? result;
      provider.pollUntilSucceeded(2).then((value) => result = value);
      async.elapse(const Duration(seconds: 7));

      expect(result?.status, PaymentStatus.succeeded);
      expect(_statusCalls(client), 3);
    });
  });

  test('poll still pending after 5 requiresAction observations', () {
    fakeAsync((async) {
      final client = _scriptedClient([]);
      final provider = _provider(client);

      RentalPaymentDto? result;
      var done = false;
      provider
          .pollUntilSucceeded(2)
          .then((value) {
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
    final client = _scriptedClient([
      (statusCode: 200, body: _intent),
      (statusCode: 200, body: _intent),
    ]);
    final provider = _provider(client);

    await provider.createIntent(2);
    await provider.createIntent(2);

    final creates = client.requests.where(
      (r) =>
          r.method == 'POST' &&
          (r).url.path.endsWith('/payments/create-intent'),
    );
    expect(creates, hasLength(2));
  });
}
