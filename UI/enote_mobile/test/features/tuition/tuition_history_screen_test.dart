import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/tuition/tuition_history_screen.dart';
import 'package:enote_mobile/features/tuition/tuition_payment_provider.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

Map<String, dynamic> _payment(int id, String status, {int? refundedCents}) => {
  'id': id,
  'enrollmentId': 5,
  'paymentIntentId': 'pi_tuition_$id',
  'amountCents': 80000,
  'currency': 'bam',
  'status': status,
  'paidAt': '2026-09-09T12:41:00',
  'periodStart': '2026-09-09T12:41:00',
  'periodEnd': '2026-10-09T12:41:00',
  'refundedAt': ?(refundedCents == null ? null : '2026-10-01T09:00:00'),
  'refundedCents': ?refundedCents,
};

void main() {
  testWidgets('shows refunded amounts and pages to the next result page', (
    tester,
  ) async {
    final client = ScriptedClient((request) {
      final page = request.url.queryParameters['page'];
      if (page == '2') {
        return jsonResponse({
          'items': [_payment(11, 'Succeeded')],
          'page': 2,
          'pageSize': 20,
          'totalCount': 40,
        }, 200);
      }
      return jsonResponse({
        'items': [
          _payment(13, 'PartiallyRefunded', refundedCents: 30000),
          _payment(12, 'Succeeded'),
        ],
        'page': 1,
        'pageSize': 20,
        'totalCount': 40,
      }, 200);
    });

    final apiClient = ApiClient(
      baseUrl: _baseUrl,
      authState: AuthState(tokenReader: () => fakeJwt(), httpClient: client),
      httpClient: client,
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<TuitionPaymentProvider>(
            create: (_) => TuitionPaymentProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(
          home: TuitionHistoryScreen(
            enrollmentId: 5,
            courseName: 'Osnove teorije muzike',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Djelomični povrat'), findsOneWidget);
    expect(find.textContaining('Povrat: 300.00 KM'), findsOneWidget);
    expect(find.text('Stranica 1 od 2'), findsOneWidget);

    await tester.tap(find.text('Sljedeća'));
    await tester.pumpAndSettle();

    expect(find.text('Stranica 2 od 2'), findsOneWidget);
    expect(find.text('800.00 KM'), findsOneWidget);
    expect(
      client.requests
          .where(
            (r) => r.url.path.endsWith('/tuition/history'),
          )
          .map((r) => r.url.queryParameters['page']),
      ['1', '2'],
    );
  });
}
