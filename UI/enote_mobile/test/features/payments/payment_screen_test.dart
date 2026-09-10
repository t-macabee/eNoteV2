import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/payments/payment_provider.dart';
import 'package:enote_mobile/features/payments/payment_screen.dart';
import 'package:enote_mobile/features/payments/payment_sheet_gateway.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';
import 'fake_payment_sheet_gateway.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';
const _rentalId = 2;

Map<String, dynamic> _rental({bool isPaid = false}) => {
  'id': _rentalId,
  'instrumentModel': 'Stratocaster',
  'instrumentType': 'Električna gitara',
  'storeName': 'Muzika d.o.o.',
  'rentalStatus': 4,
  'requestedAt': '2026-09-01T10:12:00',
  'pickedUpAt': '2026-09-03T16:30:00',
  'fee': 40.0,
  'dailyFee': 1.33,
  'monthsCharged': 0,
  'daysCharged': 6,
  'isProrated': true,
  'totalFee': 8.0,
  'isPaid': isPaid,
};

const _intent = {
  'rentalId': _rentalId,
  'paymentIntentId': 'pi_test_123',
  'clientSecret': 'pi_test_123_secret_abc',
  'amountCents': 800,
  'currency': 'bam',
  'status': 'RequiresAction',
};

Map<String, dynamic> _payment(String status) => {
  'id': 7,
  'rentalId': _rentalId,
  'paymentIntentId': 'pi_test_123',
  'amountCents': 800,
  'currency': 'bam',
  'status': status,
  'paidAt': '2026-09-09T12:41:00',
};

/// Routes by path; intent and payment queues let each test script retries
/// and poll sequences. Empty queues fall back to success shapes.
class _PayStubClient extends http.BaseClient {
  Map<String, dynamic> rental;
  final List<({int status, Map<String, dynamic> body})> intents;
  final List<({int status, Map<String, dynamic> body})> payments;

  int rentalGets = 0;
  int createCalls = 0;
  int statusCalls = 0;

  _PayStubClient({
    required this.rental,
    List<({int status, Map<String, dynamic> body})>? intents,
    List<({int status, Map<String, dynamic> body})>? payments,
  }) : intents = intents ?? [],
       payments = payments ?? [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final path = request.url.path;
    late int status;
    late String body;
    if (request.method == 'GET' && path.endsWith('/student/rentals/$_rentalId')) {
      rentalGets++;
      status = 200;
      body = jsonEncode(rental);
    } else if (path.endsWith('/payments/create-intent')) {
      createCalls++;
      final next = intents.isNotEmpty
          ? intents.removeAt(0)
          : (status: 200, body: _intent);
      status = next.status;
      body = jsonEncode(next.body);
    } else if (request.method == 'GET' && path.endsWith('/student/rentals/$_rentalId/payments')) {
      statusCalls++;
      final next = payments.isNotEmpty
          ? payments.removeAt(0)
          : (status: 200, body: _payment('Succeeded'));
      status = next.status;
      body = jsonEncode(next.body);
    } else {
      status = 404;
      body = '{}';
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      status,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _Harness {
  final _PayStubClient client;
  final FakePaymentSheetGateway gateway;
  late final AuthState authState;
  late final ApiClient apiClient;
  Object? popped;

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
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
        Provider<PaymentProvider>(
          create: (_) => PaymentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: FilledButton(
                onPressed: () => Navigator.of(context)
                    .push(
                      MaterialPageRoute(
                        builder: (_) => PaymentScreen(
                          rentalId: _rentalId,
                          gateway: gateway,
                          stripePublishableKey:
                              publishableKey ?? 'pk_test_123',
                        ),
                      ),
                    )
                    .then((value) => popped = value),
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
  _PayStubClient client,
  FakePaymentSheetGateway gateway, {
  String? publishableKey,
}) async {
  tester.view.physicalSize = const Size(400, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);

  final harness = _Harness(client: client, gateway: gateway);
  await tester.pumpWidget(
    harness.app(publishableKey: publishableKey),
  );
  await tester.tap(find.text('Open pay'));
  await tester.pumpAndSettle();
  return harness;
}

Future<void> _confirmPay(WidgetTester tester) async {
  await tester.tap(find.text('Plati 8.00 KM'));
  await tester.pumpAndSettle();
  expect(find.textContaining('Potvrda plaćanja'), findsOneWidget);
  await tester.tap(find.text('Potvrdi'));
  // Zero-duration pumps only: the dialog future and the stubbed
  // create-intent/init/present chain all complete in microtasks, while the
  // 2 s poll delays must not elapse yet (`pumpAndSettle` would advance the
  // fake clock straight through them into D or F).
  await tester.pump();
  await tester.pump();
  await tester.pump();
}

void main() {
  testWidgets('review shows the header, amount, charge and Stripe note', (
    tester,
  ) async {
    await _pumpPay(
      tester,
      _PayStubClient(rental: _rental()),
      FakePaymentSheetGateway(),
    );

    expect(find.text('Stratocaster'), findsOneWidget);
    expect(find.text('Muzika d.o.o. · Završeno'), findsOneWidget);
    expect(find.text('8.00 KM'), findsWidgets);
    expect(
      find.text('6 dana × 1.33 KM · proporcionalno'),
      findsOneWidget,
    );
    expect(
      find.text(
        'Plaćanje se obavlja putem Stripe-a. '
        'Podaci o kartici se ne pohranjuju u aplikaciji.',
      ),
      findsOneWidget,
    );
    expect(find.text('Plati 8.00 KM'), findsOneWidget);
    expect(find.textContaining('pi_test_123'), findsNothing);
  });

  testWidgets('happy path runs A to C to D', (tester) async {
    final gateway = FakePaymentSheetGateway();
    await _pumpPay(
      tester,
      _PayStubClient(rental: _rental()),
      gateway,
    );

    await _confirmPay(tester);
    expect(gateway.initCalls, 1);
    expect(gateway.lastClientSecret, 'pi_test_123_secret_abc');
    expect(gateway.presentCalls, 1);
    expect(find.text('Provjeravamo status plaćanja…'), findsOneWidget);

    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();

    expect(find.text('Plaćanje uspješno'), findsOneWidget);
    expect(find.text('8.00 KM · 09.09.2026. 12:41'), findsOneWidget);
    expect(find.text('Iznajmljivanje je izmireno.'), findsOneWidget);
    expect(find.text('Nazad na iznajmljivanje'), findsOneWidget);
  });

  testWidgets('leaving through D refetches the rental and pops true', (
    tester,
  ) async {
    final client = _PayStubClient(rental: _rental());
    final harness = await _pumpPay(tester, client, FakePaymentSheetGateway());

    await _confirmPay(tester);
    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Nazad na iznajmljivanje'));
    await tester.pumpAndSettle();

    expect(harness.popped, isTrue);
    expect(client.rentalGets, 2);
  });

  testWidgets('a dismissed sheet shows E with the cancelled copy', (
    tester,
  ) async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    await _pumpPay(
      tester,
      _PayStubClient(rental: _rental()),
      gateway,
    );

    await _confirmPay(tester);

    expect(find.text('Plaćanje nije dovršeno'), findsOneWidget);
    expect(find.text('Plaćanje je otkazano.'), findsOneWidget);
    expect(find.text('Pokušaj ponovo'), findsOneWidget);
    expect(find.text('Odustani'), findsOneWidget);
  });

  testWidgets('a failed sheet shows E with the Stripe message', (
    tester,
  ) async {
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetFailure('Your card was declined.'));
    await _pumpPay(
      tester,
      _PayStubClient(rental: _rental()),
      gateway,
    );

    await _confirmPay(tester);

    expect(find.text('Plaćanje nije dovršeno'), findsOneWidget);
    expect(find.text('Your card was declined.'), findsOneWidget);
  });

  testWidgets('Pokusaj ponovo calls create-intent again', (tester) async {
    final client = _PayStubClient(rental: _rental());
    final gateway = FakePaymentSheetGateway()
      ..queuePresent(PaymentSheetCancelled());
    await _pumpPay(tester, client, gateway);

    await _confirmPay(tester);
    expect(client.createCalls, 1);

    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pump();
    await tester.pump();
    await tester.pump();
    expect(client.createCalls, 2);
    expect(find.text('Provjeravamo status plaćanja…'), findsOneWidget);

    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();
    expect(find.text('Plaćanje uspješno'), findsOneWidget);
  });

  testWidgets('still pending after the poll window shows F', (tester) async {
    final client = _PayStubClient(
      rental: _rental(),
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
    expect(
      find.text('Provjerite ponovo za nekoliko sekundi.'),
      findsOneWidget,
    );
    expect(find.text('Provjeri ponovo'), findsOneWidget);
    expect(find.text('Nazad'), findsOneWidget);
    expect(client.statusCalls, 5);
  });

  testWidgets('Provjeri ponovo and the deep-link recheck run one more cycle', (
    tester,
  ) async {
    final client = _PayStubClient(
      rental: _rental(),
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

    PaymentScreen.stripeRedirectHandler?.call();
    await tester.pump(const Duration(seconds: 11));
    await tester.pumpAndSettle();
    expect(client.statusCalls, 10);

    await tester.tap(find.text('Provjeri ponovo'));
    await tester.pump(const Duration(seconds: 11));
    await tester.pumpAndSettle();
    expect(client.statusCalls, 15);
  });

  testWidgets('a paid rental is refused with the already-paid copy', (
    tester,
  ) async {
    await _pumpPay(
      tester,
      _PayStubClient(rental: _rental(isPaid: true)),
      FakePaymentSheetGateway(),
    );

    expect(
      find.text('Ovo iznajmljivanje je već plaćeno.'),
      findsOneWidget,
    );
    expect(find.textContaining('Plati'), findsNothing);
  });

  testWidgets('a server refusal renders verbatim in E', (tester) async {
    final gateway = FakePaymentSheetGateway();
    await _pumpPay(
      tester,
      _PayStubClient(
        rental: _rental(),
        intents: [
          (
            status: 400,
            body: {'message': 'Stavka je već plaćena.'},
          ),
        ],
      ),
      gateway,
    );

    await _confirmPay(tester);

    expect(find.text('Plaćanje nije dovršeno'), findsOneWidget);
    expect(find.text('Stavka je već plaćena.'), findsOneWidget);
    expect(gateway.presentCalls, 0);
  });

  testWidgets('an empty publishable key shows the unavailable copy, no sheet', (
    tester,
  ) async {
    final gateway = FakePaymentSheetGateway();
    await _pumpPay(
      tester,
      _PayStubClient(rental: _rental()),
      gateway,
      publishableKey: '',
    );

    await _confirmPay(tester);

    expect(
      find.text('Plaćanje trenutno nije dostupno. Pokušajte ponovo kasnije.'),
      findsOneWidget,
    );
    expect(gateway.presentCalls, 0);
  });
}
