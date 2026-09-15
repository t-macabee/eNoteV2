import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/payments/payment_sheet_gateway.dart';
import 'package:enote_mobile/features/tuition/tuition_payment_provider.dart';
import 'package:enote_mobile/features/tuition/tuition_payment_screen.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';
import '../payments/fake_payment_sheet_gateway.dart';

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

/// Routes by path; intent and payment queues let each test script retries
/// and poll sequences. Empty queues fall back to success shapes.
class _TuitionStubClient extends http.BaseClient {
  final List<({int status, Object body})> intents;
  final List<({int status, Object body})> payments;

  int createCalls = 0;
  int statusCalls = 0;

  _TuitionStubClient({
    List<({int status, Object body})>? intents,
    List<({int status, Object body})>? payments,
  }) : intents = intents ?? [],
       payments = payments ?? [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    late int status;
    late Object body;
    if (path.endsWith('/tuition/create-intent')) {
      createCalls++;
      final next = intents.isNotEmpty
          ? intents.removeAt(0)
          : (status: 200, body: _intent);
      status = next.status;
      body = next.body;
    } else if (request.method == 'GET' &&
        path.endsWith('/student/enrollments/$_enrollmentId/tuition')) {
      statusCalls++;
      final next = payments.isNotEmpty
          ? payments.removeAt(0)
          : (status: 200, body: _payment('Succeeded'));
      status = next.status;
      body = next.body;
    } else {
      status = 404;
      body = const {};
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      status,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _Harness {
  final _TuitionStubClient client;
  final FakePaymentSheetGateway gateway;
  late final AuthState authState;
  late final ApiClient apiClient;
  _Harness({required this.client, required this.gateway}) {
    authState = AuthState(
      baseUrl: _baseUrl,
      tokenReader: () => fakeJwt(),
      httpClient: client,
    );
    apiClient = ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: client,
    );
  }

  Widget app({String? publishableKey}) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        Provider<TuitionPaymentProvider>(
          create: (_) => TuitionPaymentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: FilledButton(
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => TuitionPaymentScreen(
                      enrollmentId: _enrollmentId,
                      courseName: 'Osnove teorije muzike',
                      price: 800.0,
                      gateway: gateway,
                      stripePublishableKey: publishableKey ?? 'pk_test_123',
                    ),
                  ),
                ),
                child: const Text('Open pay'),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

Future<_Harness> _pumpPay(
  WidgetTester tester,
  _TuitionStubClient client,
  FakePaymentSheetGateway gateway, {
  String? publishableKey,
}) async {
  tester.view.physicalSize = const Size(400, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);

  final harness = _Harness(client: client, gateway: gateway);
  await tester.pumpWidget(harness.app(publishableKey: publishableKey));
  await tester.tap(find.text('Open pay'));
  await tester.pumpAndSettle();
  return harness;
}

Future<void> _confirmPay(WidgetTester tester) async {
  await tester.tap(find.text('Plati 800.00 KM'));
  await tester.pumpAndSettle();
  expect(find.textContaining('Potvrda plaćanja'), findsOneWidget);
  await tester.tap(find.text('Potvrdi'));
  await tester.pump();
  await tester.pump();
  await tester.pump();
}

void main() {
  tearDown(() {
    TuitionPaymentScreen.stripeRedirectHandler = null;
  });

  testWidgets('review shows the course, amount, period and Stripe note', (
    tester,
  ) async {
    await _pumpPay(tester, _TuitionStubClient(), FakePaymentSheetGateway());

    expect(find.text('Osnove teorije muzike'), findsOneWidget);
    expect(find.text('800.00 KM'), findsWidgets);
    expect(find.text('30 dana'), findsOneWidget);
    expect(
      find.text(
        'Plaćanje se obavlja putem Stripe-a. '
        'Podaci o kartici se ne pohranjuju u aplikaciji.',
      ),
      findsOneWidget,
    );
    expect(find.text('Plati 800.00 KM'), findsOneWidget);
    expect(find.textContaining('pi_tuition_123'), findsNothing);
  });

  testWidgets('happy path runs A to C to D with the tuition copy', (
    tester,
  ) async {
    final gateway = FakePaymentSheetGateway();
    await _pumpPay(tester, _TuitionStubClient(), gateway);

    await _confirmPay(tester);
    expect(gateway.initCalls, 1);
    expect(gateway.lastClientSecret, 'pi_tuition_123_secret_abc');
    expect(gateway.presentCalls, 1);
    expect(find.text('Provjeravamo status plaćanja…'), findsOneWidget);

    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();

    expect(find.text('Plaćanje uspješno'), findsOneWidget);
    expect(find.text('800.00 KM · 09.09.2026. 12:41'), findsOneWidget);
    expect(find.text('Školarina je plaćena.'), findsOneWidget);
    expect(find.text('Nazad na kurs'), findsOneWidget);
  });

  testWidgets('leaving through D pops back to the caller', (tester) async {
    final client = _TuitionStubClient();
    await _pumpPay(tester, client, FakePaymentSheetGateway());

    await _confirmPay(tester);
    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Nazad na kurs'));
    await tester.pumpAndSettle();

    expect(find.text('Plaćanje školarine'), findsNothing);
    expect(find.text('Open pay'), findsOneWidget);
    expect(client.createCalls, 1);
  });

  testWidgets('a dismissed sheet shows E with the cancelled copy', (
    tester,
  ) async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    await _pumpPay(tester, _TuitionStubClient(), gateway);

    await _confirmPay(tester);

    expect(find.text('Plaćanje nije dovršeno'), findsOneWidget);
    expect(find.text('Plaćanje je otkazano.'), findsOneWidget);
    expect(find.text('Pokušaj ponovo'), findsOneWidget);
  });

  testWidgets('a decline-then-dismiss shows the declined copy', (tester) async {
    final client = _TuitionStubClient(
      payments: [(status: 200, body: _payment('Failed'))],
    );
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    await _pumpPay(tester, client, gateway);

    await _confirmPay(tester);
    await tester.pump();

    expect(find.text('Kartica je odbijena.'), findsOneWidget);
    expect(find.text('Plaćanje je otkazano.'), findsNothing);
    expect(client.statusCalls, 1);
  });

  testWidgets('a failed sheet shows E with the Stripe message', (tester) async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetFailure('Your card was declined.'));
    await _pumpPay(tester, _TuitionStubClient(), gateway);

    await _confirmPay(tester);

    expect(find.text('Plaćanje nije dovršeno'), findsOneWidget);
    expect(find.text('Your card was declined.'), findsOneWidget);
  });

  testWidgets('still pending after the poll window shows F', (tester) async {
    final client = _TuitionStubClient(
      payments: List.generate(
        10,
        (_) => (status: 200, body: _payment('RequiresAction')),
      ),
    );
    await _pumpPay(tester, client, FakePaymentSheetGateway());

    await _confirmPay(tester);
    await tester.pump(const Duration(seconds: 11));
    await tester.pumpAndSettle();

    expect(find.text('Plaćanje se obrađuje'), findsOneWidget);
    expect(find.text('Provjeri ponovo'), findsOneWidget);
    expect(client.statusCalls, 5);
  });

  testWidgets('the deep-link recheck runs one more cycle', (tester) async {
    final client = _TuitionStubClient(
      payments: List.generate(
        15,
        (_) => (status: 200, body: _payment('RequiresAction')),
      ),
    );
    await _pumpPay(tester, client, FakePaymentSheetGateway());

    await _confirmPay(tester);
    await tester.pump(const Duration(seconds: 11));
    await tester.pumpAndSettle();
    expect(client.statusCalls, 5);

    TuitionPaymentScreen.stripeRedirectHandler?.call();
    await tester.pump(const Duration(seconds: 11));
    await tester.pumpAndSettle();
    expect(client.statusCalls, 10);
  });

  testWidgets('popping the screen clears the redirect handler', (tester) async {
    await _pumpPay(tester, _TuitionStubClient(), FakePaymentSheetGateway());

    expect(TuitionPaymentScreen.stripeRedirectHandler, isNotNull);

    Navigator.of(tester.element(find.text('Osnove teorije muzike'))).pop();
    await tester.pumpAndSettle();

    expect(TuitionPaymentScreen.stripeRedirectHandler, isNull);
  });

  testWidgets('a server refusal renders verbatim in E', (tester) async {
    final gateway = FakePaymentSheetGateway();
    await _pumpPay(
      tester,
      _TuitionStubClient(
        intents: [
          (status: 400, body: {'message': 'Kurs je besplatan.'}),
        ],
      ),
      gateway,
    );

    await _confirmPay(tester);

    expect(find.text('Plaćanje nije dovršeno'), findsOneWidget);
    expect(find.text('Kurs je besplatan.'), findsOneWidget);
    expect(gateway.presentCalls, 0);
  });

  testWidgets('an empty publishable key shows the unavailable copy, no sheet', (
    tester,
  ) async {
    final gateway = FakePaymentSheetGateway();
    await _pumpPay(tester, _TuitionStubClient(), gateway, publishableKey: '');

    await _confirmPay(tester);

    expect(
      find.text('Plaćanje trenutno nije dostupno. Pokušajte ponovo kasnije.'),
      findsOneWidget,
    );
    expect(gateway.presentCalls, 0);
  });
}
