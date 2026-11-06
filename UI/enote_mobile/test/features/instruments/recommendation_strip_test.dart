import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/recommendation_strip.dart';
import 'package:enote_mobile/widgets/app_sheet_body.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

const _longReason =
    'Preporučeno na osnovu vaše historije najma i instrumenta koje su '
    'studenti sa sličnim izborima iznajmljivali prije vas.';

InstrumentRecommendationDto _recommendation({
  int id = 1,
  String manufacturer = 'Yamaha',
  String model = 'C40',
  List<String> reasons = const [],
}) {
  return InstrumentRecommendationDto(
    instrument: InstrumentDto.fromJson({
      'id': id,
      'model': model,
      'manufacturer': manufacturer,
      'instrumentTypeId': 1,
      'instrumentType': 'Klasična gitara',
      'monthlyFee': 45.0,
      'musicStore': 'Muzika d.o.o.',
      'isAvailable': true,
    }),
    score: 0.645,
    reasons: reasons,
  );
}

Future<void> _pumpStrip(
  WidgetTester tester,
  List<InstrumentRecommendationDto> recommendations, {
  void Function(InstrumentDto instrument)? onOpen,
}) async {
  tester.view.physicalSize = const Size(400, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);

  final client = ScriptedClient((_) => jsonResponse('', 204));
  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: _baseUrl,
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    Provider<ApiClient>.value(
      value: apiClient,
      child: MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: RecommendationStrip(
              recommendations: recommendations,
              onOpen: onOpen ?? (_) {},
            ),
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('an empty list draws no heading', (tester) async {
    await _pumpStrip(tester, const []);

    expect(find.text('PREPORUČENO ZA VAS'), findsNothing);
  });

  testWidgets('a single long reason opens in the sheet', (tester) async {
    await _pumpStrip(tester, [
      _recommendation(reasons: const [_longReason]),
    ]);

    expect(find.text('Zašto?'), findsOneWidget);

    await tester.tap(find.text('Zašto?'));
    await tester.pumpAndSettle();

    expect(
      find.descendant(
        of: find.byType(AppSheetBody),
        matching: find.text(_longReason),
      ),
      findsOneWidget,
    );
  });

  testWidgets('the sheet lists all three reasons', (tester) async {
    const reasons = [
      'Pregledali ste ovaj instrument.',
      'Popularan među studentima.',
      'Sličan vašim prethodnim izborima proizvođača ili vrste.',
    ];
    await _pumpStrip(tester, [_recommendation(reasons: reasons)]);

    await tester.tap(find.text('Zašto?'));
    await tester.pumpAndSettle();

    for (final reason in reasons) {
      expect(
        find.descendant(
          of: find.byType(AppSheetBody),
          matching: find.text(reason),
        ),
        findsOneWidget,
      );
    }
  });

  testWidgets('tapping the button does not open the instrument', (
    tester,
  ) async {
    final opened = <InstrumentDto>[];
    await _pumpStrip(tester, [
      _recommendation(reasons: const [_longReason]),
    ], onOpen: opened.add);

    await tester.tap(find.text('Zašto?'));
    await tester.pumpAndSettle();

    expect(opened, isEmpty);
  });

  testWidgets('tapping the card opens the instrument', (tester) async {
    final opened = <InstrumentDto>[];
    await _pumpStrip(tester, [
      _recommendation(reasons: const [_longReason]),
    ], onOpen: opened.add);

    await tester.tap(find.text('Yamaha C40'));
    await tester.pumpAndSettle();

    expect(opened, hasLength(1));
    expect(opened.single.id, 1);
  });
}
