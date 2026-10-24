import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/payments/rental_payment_provider.dart';
import 'package:enote_mobile/features/rentals/rental_detail_screen.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/shell/app_router.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

Map<String, dynamic> _rentalJson({required bool instrumentIsActive}) => {
  'id': 12,
  'instrumentId': 3,
  'musicStoreId': 1,
  'studentProfileId': 1,
  'studentUserId': 3,
  'instrumentModel': 'Stratocaster',
  'instrumentType': 'Električna gitara',
  'storeName': 'Muzika d.o.o.',
  'instrumentIsActive': instrumentIsActive,
  'rentalStatus': 'Completed',
  'requestedAt': '2026-09-01T10:12:00',
  'fee': 40.0,
  'totalFee': 40.0,
  'isPaid': false,
};

class _FixedRentalProvider extends RentalProvider {
  final InstrumentRentalDto rental;

  _FixedRentalProvider({required super.apiClient, required this.rental});

  @override
  Future<InstrumentRentalDto> getById(int id) async => rental;
}

class _NoPaymentProvider extends RentalPaymentProvider {
  _NoPaymentProvider({required super.apiClient});

  @override
  Future<RentalPaymentDto?> status(int rentalId) async => null;
}

class _NavigatorSpy extends NavigatorObserver {
  final List<Route<dynamic>> pushed = [];

  @override
  void didPush(Route<dynamic> route, Route<dynamic>? previousRoute) {
    pushed.add(route);
    super.didPush(route, previousRoute);
  }
}

void main() {
  Future<_NavigatorSpy> pumpDetail(
    WidgetTester tester, {
    required bool instrumentIsActive,
  }) async {
    final httpClient = ScriptedClient((_) => throw UnimplementedError());
    final authState = AuthState(
      baseUrl: _baseUrl,
      tokenReader: () => fakeJwt(),
      httpClient: httpClient,
    );
    final apiClient = ApiClient(
      baseUrl: _baseUrl,
      authState: authState,
      httpClient: httpClient,
    );
    final rental = InstrumentRentalDto.fromJson(
      _rentalJson(instrumentIsActive: instrumentIsActive),
    );
    final observer = _NavigatorSpy();

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<RentalProvider>.value(
            value: _FixedRentalProvider(apiClient: apiClient, rental: rental),
          ),
          Provider<RentalPaymentProvider>.value(
            value: _NoPaymentProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          navigatorObservers: [observer],
          home: const RentalDetailScreen(rentalId: 12),
          onGenerateRoute: (settings) => MaterialPageRoute<void>(
            settings: settings,
            builder: (_) => const Scaffold(body: Text('Instrument')),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    return observer;
  }

  testWidgets('archived instrument is announced and does not open the catalog',
      (tester) async {
    final observer = await pumpDetail(tester, instrumentIsActive: false);

    expect(find.text('Instrument više nije u ponudi'), findsOneWidget);

    await tester.tap(find.text('Stratocaster'));
    await tester.pumpAndSettle();

    expect(observer.pushed, hasLength(1));
    expect(
      observer.pushed.where(
        (route) => route.settings.name == AppRouter.instrumentDetail,
      ),
      isEmpty,
    );
  });

  testWidgets('active instrument opens the catalog page', (tester) async {
    final observer = await pumpDetail(tester, instrumentIsActive: true);

    expect(find.text('Instrument više nije u ponudi'), findsNothing);

    await tester.tap(find.text('Stratocaster'));
    await tester.pumpAndSettle();

    expect(
      observer.pushed.where(
        (route) => route.settings.name == AppRouter.instrumentDetail,
      ),
      hasLength(1),
    );
  });
}
