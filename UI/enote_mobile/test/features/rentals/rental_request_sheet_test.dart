import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/features/rentals/rental_request_sheet.dart';
import 'package:enote_mobile/shell/routes.dart';
import 'package:enote_mobile/widgets/app_text_field.dart';

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
  group('rentalRequestSentMessage', () {
    test('mentions the other pending requests when there are some', () {
      expect(
        rentalRequestSentMessage(2),
        'Zahtjev za iznajmljivanje je poslan. '
        'Broj zahtjeva na čekanju za isti instrument: 2.',
      );
    });

    test('is the plain confirmation when nobody else waits', () {
      const plain = 'Zahtjev za iznajmljivanje je poslan.';
      expect(rentalRequestSentMessage(0), plain);
      expect(rentalRequestSentMessage(null), plain);
    });
  });

  test('InstrumentRentalDto reads otherPendingRequests from the response', () {
    expect(InstrumentRentalDto.fromJson(_rental).otherPendingRequests, isNull);
    expect(
      InstrumentRentalDto.fromJson({..._rental, 'otherPendingRequests': 3})
          .otherPendingRequests,
      3,
    );
  });

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
    },
  );

  testWidgets(
    'request sheet sends without a second dialog and opens the rental detail',
    (tester) async {
      final client = ScriptedClient((request) {
        if (request.method == 'POST') {
          return jsonResponse(jsonEncode(_rental), 201);
        }
        throw UnimplementedError();
      });
      final authState = AuthState(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        tokenReader: () => fakeJwt(),
        httpClient: client,
      );
      final apiClient = ApiClient(
        baseUrl: 'http://10.0.2.2:5059/api/v1/',
        authState: authState,
        httpClient: client,
      );
      final pushed = <RouteSettings>[];

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthState>.value(value: authState),
            Provider<ApiClient>.value(value: apiClient),
            ChangeNotifierProvider<RentalProvider>(
              create: (_) => RentalProvider(apiClient: apiClient),
            ),
          ],
          child: MaterialApp(
            onGenerateRoute: (settings) {
              pushed.add(settings);
              return MaterialPageRoute<void>(
                settings: settings,
                builder: (_) => const Scaffold(body: Text('Rental detail')),
              );
            },
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
      await tester.tap(find.text('Pošalji zahtjev'));
      await tester.pumpAndSettle();

      expect(find.text('Potvrdi'), findsNothing);
      expect(find.byType(RentalRequestSheet), findsNothing);
      expect(find.text('Rental detail'), findsOneWidget);
      expect(pushed.last.name, AppRoutes.rentalDetail);
      expect((pushed.last.arguments as RentalDetailArgs).rentalId, 12);
    },
  );

  testWidgets('note field is an AppTextField with a 500 character limit', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: RentalRequestSheet(instrument: _instrument)),
      ),
    );

    final field = tester.widget<AppTextField>(find.byType(AppTextField));
    expect(field.label, 'Napomena');
    expect(field.maxLength, RentalRequestSheet.noteMaxLength);
    expect(field.minLines, 3);
    expect(field.maxLines, 5);
    expect(find.byType(TextFormField), findsOneWidget);

    await tester.enterText(find.byType(TextFormField), 'Potreban za nastavu.');
    await tester.pump();
    expect(find.text('Potreban za nastavu.'), findsOneWidget);
    expect(find.text('0/500'), findsNothing);
    expect(find.text('20/500'), findsOneWidget);
  });
}
