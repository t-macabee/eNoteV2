import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/features/rentals/rental_request_sheet.dart';

import '../../helpers.dart';

final _instrument = InstrumentDto(
  id: 3,
  model: 'Stratocaster',
  manufacturer: 'Fender',
  instrumentTypeId: 1,
  instrumentType: 'Električna gitara',
  musicStore: 'Muzika d.o.o.',
  isAvailable: true,
);

const _rental = {
  'id': 12,
  'instrumentId': 3,
  'musicStoreId': 1,
  'studentProfileId': 1,
  'studentUserId': 3,
  'instrumentModel': 'Stratocaster',
  'instrumentType': 'Električna gitara',
  'storeName': 'Muzika d.o.o.',
  'rentalStatus': 'Pending',
  'requestedAt': '2026-09-01T10:12:00',
  'fee': 40.0,
  'isPaid': false,
};

class _GatedRentalProvider extends RentalProvider {
  Completer<InstrumentRentalDto>? requestGate;

  _GatedRentalProvider({required super.apiClient});

  @override
  Future<InstrumentRentalDto> createRequest(RentalCreateRequest request) {
    requestGate ??= Completer<InstrumentRentalDto>();
    return requestGate!.future;
  }
}

void main() {
  testWidgets(
      'completing the request after the sheet is dismissed pops nothing',
      (tester) async {
    final noop = ScriptedClient((_) => throw UnimplementedError());
    final authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      httpClient: noop,
    );
    final apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: noop,
    );
    final rentals = _GatedRentalProvider(apiClient: apiClient);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthState>.value(value: authState),
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<RentalProvider>.value(value: rentals),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => Center(
                child: FilledButton(
                  onPressed: () =>
                      showRentalRequestSheet(context, _instrument),
                  child: const Text('Open sheet'),
                ),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open sheet'));
    await tester.pumpAndSettle();
    expect(find.byType(RentalRequestSheet), findsOneWidget);

    await tester.tap(find.text('Pošalji zahtjev'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Potvrdi'));
    // No settle: the submit button shows an indeterminate spinner while the
    // create request is pending.
    await tester.pump();
    await tester.pump();

    Navigator.of(tester.element(find.byType(RentalRequestSheet))).pop();
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.byType(RentalRequestSheet), findsNothing);

    rentals.requestGate!.complete(InstrumentRentalDto.fromJson(_rental));
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('Open sheet'), findsOneWidget);
  });
}
