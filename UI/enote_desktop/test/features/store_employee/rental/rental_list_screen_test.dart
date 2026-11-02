import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/theme/app_theme.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_detail_dialog.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_list_screen.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';
import 'package:enote_desktop/widgets/async_dropdown.dart';

import '../../../helpers.dart';

Map<String, dynamic> _rentalItem([Map<String, dynamic> extra = const {}]) => {
  'id': 7,
  'instrumentId': 1,
  'musicStoreId': 1,
  'studentProfileId': 1,
  'studentUserId': 2,
  'studentName': 'Student Test',
  'instrumentModel': 'Stratocaster',
  'instrumentType': 'Gitara',
  'storeName': 'Shop',
  'rentalStatus': 'Active',
  'requestedAt': DateTime(2026, 1, 5).toIso8601String(),
  'fee': 10.0,
  'totalFee': 5.0,
  'isPaid': false,
  ...extra,
};

ScriptedClient _client([Map<String, dynamic> extra = const {}]) =>
    ScriptedClient((request) {
      final path = request.url.path;
      if (request.method == 'GET' && path.endsWith('/shop/rentals/7')) {
        return jsonResponse(_rentalItem(extra), 200);
      }
      if (request.method == 'GET' && path.contains('shop/instruments')) {
        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 100,
          'totalCount': 0,
        }, 200);
      }
      if (request.method == 'GET' && path.contains('shop/rentals')) {
        return jsonResponse({
          'items': [_rentalItem(extra)],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        }, 200);
      }
      return jsonResponse(const {}, 200);
    });

Future<void> _pumpList(
  WidgetTester tester,
  ScriptedClient client, {
  Size size = const Size(1400, 1000),
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: client,
    tokenReader: () => fakeJwt(username: 'test', role: 'StoreEmployee'),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: client,
  );

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<InstrumentProvider>.value(
          value: InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>.value(
          value: RentalProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: desktopTheme,
        home: const Scaffold(body: RentalListScreen()),
      ),
    ),
  );
  await tester.pump();
  await pumpPastDebounce(tester);
  await tester.pumpAndSettle();
}

ScriptedClient _statusClient({
  Map<String, dynamic> extra = const {},
  int pendingCount = 3,
  int listTotalCount = 1,
  List<Map<String, dynamic>> instruments = const [],
}) => ScriptedClient((request) {
  final path = request.url.path;
  final query = request.url.queryParameters;
  if (request.method == 'GET' && path.endsWith('/shop/rentals/7')) {
    return jsonResponse(_rentalItem(extra), 200);
  }
  if (request.method == 'GET' && path.contains('shop/instruments')) {
    return jsonResponse({
      'items': instruments,
      'page': 1,
      'pageSize': 100,
      'totalCount': instruments.length,
    }, 200);
  }
  if (request.method == 'GET' && path.contains('shop/rentals')) {
    final pageSize = int.parse(query['pageSize'] ?? '20');
    final isCount = pageSize == 1;
    return jsonResponse({
      'items': isCount ? const [] : [_rentalItem(extra)],
      'page': int.parse(query['page'] ?? '1'),
      'pageSize': pageSize,
      'totalCount': isCount ? pendingCount : listTotalCount,
    }, 200);
  }
  if (request.method == 'POST') {
    return jsonResponse(_rentalItem(extra), 200);
  }
  return jsonResponse(const {}, 200);
});

List<Uri> _listRequests(ScriptedClient client) => client.getUrls
    .map(Uri.parse)
    .where(
      (u) =>
          u.path.contains('shop/rentals') &&
          u.queryParameters['pageSize'] == '20',
    )
    .toList();

List<Uri> _countRequests(ScriptedClient client) => client.getUrls
    .map(Uri.parse)
    .where(
      (u) =>
          u.path.contains('shop/rentals') &&
          u.queryParameters['pageSize'] == '1',
    )
    .toList();

void main() {
  testWidgets(
    'F4-04: instrument filter query matches pagedQuery(1, 100) output',
    (tester) async {
      final client = _client();
      await _pumpList(tester, client);

      final instrumentUrls = client.getUrls
          .where((u) => u.contains('shop/instruments'))
          .toList();
      expect(instrumentUrls, isNotEmpty);
      final expected = pagedQuery(
        1,
        100,
        '',
      ).map((k, v) => MapEntry(k, v.toString()));
      expect(Uri.parse(instrumentUrls.first).queryParameters, equals(expected));
    },
  );

  testWidgets('F4-05: fee column renders with the KM suffix', (tester) async {
    final client = _client();
    await _pumpList(tester, client);

    expect(find.text('10.00 KM / 5.00 KM'), findsOneWidget);
  });

  testWidgets(
    'row tap opens the rental detail dialog and the row has no edit button',
    (tester) async {
      await _pumpList(tester, _statusClient(), size: const Size(1280, 800));

      expect(find.byIcon(Icons.edit), findsNothing);

      await tester.tap(find.text('Stratocaster'));
      await tester.pumpAndSettle();

      expect(find.byType(Dialog), findsOneWidget);
      expect(find.byType(RentalDetailDialog), findsOneWidget);
    },
  );

  testWidgets('the instrument filter loads every page of instruments', (
    tester,
  ) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.contains('shop/instruments')) {
        final page = int.parse(request.url.queryParameters['page'] ?? '1');
        return jsonResponse({
          'items': [
            for (var i = 0; i < (page == 1 ? 100 : 50); i++)
              {
                'id': page * 1000 + i,
                'model': 'Instrument ${page * 1000 + i}',
                'manufacturer': 'Fender',
                'instrumentTypeId': 1,
                'instrumentType': 'Gitara',
                'musicStore': 'Shop',
                'isAvailable': true,
              },
          ],
          'page': page,
          'pageSize': 100,
          'totalCount': 150,
        }, 200);
      }
      if (path.contains('shop/rentals')) {
        return jsonResponse(const {
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }, 200);
      }
      return jsonResponse(const {}, 200);
    });

    await _pumpList(tester, client);

    final instrumentUrls = client.getUrls
        .where((u) => u.contains('shop/instruments'))
        .toList();
    expect(instrumentUrls, hasLength(2));
    expect(instrumentUrls.first, contains('page=1'));
    expect(instrumentUrls.last, contains('page=2'));

    final dropdown = tester.widget<DropdownButton<Object>>(
      find.byType(DropdownButton<Object>),
    );
    expect(dropdown.items, hasLength(151));
    expect((dropdown.items!.first.child as Text).data, 'Svi instrumenti');
    expect((dropdown.items!.last.child as Text).data, 'Instrument 2049');
  });

  testWidgets(
    'overdue active row appends the overdue days in the danger chip',
    (tester) async {
      await _pumpList(
        tester,
        _client({
          'dueAt': DateTime.now()
              .subtract(const Duration(days: 2))
              .toIso8601String(),
          'overdueDays': 4,
        }),
      );

      expect(find.text('Aktivno · kasni 4 dana'), findsOneWidget);
      final label = tester.widget<Text>(find.text('Aktivno · kasni 4 dana'));
      expect(label.style?.color, AppTheme.error);
    },
  );

  testWidgets('completed row with overdue days shows the plain status label', (
    tester,
  ) async {
    await _pumpList(
      tester,
      _client({'rentalStatus': 'Completed', 'overdueDays': 2}),
    );

    final statusLabel = find.descendant(
      of: find.byType(DataTable),
      matching: find.text('Završeno'),
    );

    expect(statusLabel, findsOneWidget);
    expect(find.textContaining('kasni'), findsNothing);

    final label = tester.widget<Text>(statusLabel);
    expect(
      label.style?.color,
      rentalStatusTone(InstrumentRentalStatus.completed).color,
    );
    expect(label.style?.color, isNot(Colors.red));
  });

  testWidgets('changing the status tab resets to page 1', (tester) async {
    final client = _statusClient(listTotalCount: 40);
    await _pumpList(tester, client, size: const Size(1280, 800));

    await tester.tap(find.text('Sljedeća'));
    await tester.pumpAndSettle();
    expect(_listRequests(client).last.queryParameters['page'], '2');

    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    expect(_listRequests(client).last.queryParameters['page'], '1');
  });

  testWidgets('pending count request includes the selected instrument', (
    tester,
  ) async {
    final client = _statusClient(
      instruments: [
        {
          'id': 1,
          'model': 'Stratocaster',
          'manufacturer': 'Fender',
          'instrumentTypeId': 1,
          'instrumentType': 'Gitara',
          'musicStore': 'Shop',
          'isAvailable': true,
        },
      ],
    );
    await _pumpList(tester, client, size: const Size(1280, 800));

    await tester.tap(find.byType(DropdownButton<Object>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Stratocaster').last);
    await tester.pumpAndSettle();

    expect(_countRequests(client).last.queryParameters['instrumentId'], '1');
  });

  testWidgets('opens on the Na čekanju tab with the pending filter', (
    tester,
  ) async {
    final client = _statusClient();
    await _pumpList(tester, client, size: const Size(1280, 800));

    final chip = tester.widget<ChoiceChip>(
      find.widgetWithText(ChoiceChip, 'Na čekanju (3)'),
    );
    expect(chip.selected, isTrue);
    expect(
      _listRequests(client).last.queryParameters['rentalStatus'],
      'Pending',
    );
  });

  testWidgets('Na čekanju tab shows the pending count', (tester) async {
    await _pumpList(
      tester,
      _statusClient(pendingCount: 7),
      size: const Size(1280, 800),
    );

    expect(find.text('Na čekanju (7)'), findsOneWidget);
  });

  testWidgets('Svi tab sends no status filter', (tester) async {
    final client = _statusClient();
    await _pumpList(tester, client, size: const Size(1280, 800));

    await tester.tap(find.text('Svi'));
    await tester.pumpAndSettle();

    expect(
      _listRequests(client).last.queryParameters.containsKey('rentalStatus'),
      isFalse,
    );
  });

  testWidgets('pending row shows Odobri and Odbij', (tester) async {
    await _pumpList(
      tester,
      _statusClient(extra: {'rentalStatus': 'Pending'}),
      size: const Size(1280, 800),
    );

    expect(find.text('Odobri'), findsOneWidget);
    expect(find.text('Odbij'), findsOneWidget);
  });

  testWidgets('non-pending row shows no Odobri or Odbij', (tester) async {
    await _pumpList(tester, _statusClient(), size: const Size(1280, 800));

    expect(find.text('Odobri'), findsNothing);
    expect(find.text('Odbij'), findsNothing);
  });

  testWidgets(
    'Odobri confirms and POSTs approve, then reloads the list and the count',
    (tester) async {
      final client = _statusClient(extra: {'rentalStatus': 'Pending'});
      await _pumpList(tester, client, size: const Size(1280, 800));

      final listBefore = _listRequests(client).length;
      final countBefore = _countRequests(client).length;

      await tester.tap(find.text('Odobri'));
      await tester.pumpAndSettle();

      expect(
        find.text('Odobriti ovaj zahtjev za iznajmljivanje?'),
        findsOneWidget,
      );

      await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
      await tester.pumpAndSettle();

      expect(
        client.postUrls.where((u) => u.endsWith('/shop/rentals/7/approve')),
        hasLength(1),
      );
      expect(_listRequests(client).length, listBefore + 1);
      expect(_countRequests(client).length, countBefore + 1);
    },
  );

  testWidgets('Odbij requires a reason and POSTs it in the body', (
    tester,
  ) async {
    final client = _statusClient(extra: {'rentalStatus': 'Pending'});
    await _pumpList(tester, client, size: const Size(1280, 800));

    await tester.tap(find.text('Odbij'));
    await tester.pumpAndSettle();

    expect(find.text('Razlog *'), findsOneWidget);
    final confirmButton = tester.widget<ElevatedButton>(
      find.widgetWithText(ElevatedButton, 'Potvrdi'),
    );
    expect(confirmButton.onPressed, isNull);

    await tester.enterText(find.byType(TextField), 'Instrument je oštećen');
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      client.postUrls.where((u) => u.endsWith('/shop/rentals/7/reject')),
      hasLength(1),
    );
    final posted = client.recorded
        .where((request) => request.method == 'POST')
        .last;
    expect(posted.body?['note'], 'Instrument je oštećen');
  });

  testWidgets('status tabs fit in one row', (tester) async {
    await _pumpList(tester, _statusClient(), size: const Size(1280, 800));

    final chips = find.byType(ChoiceChip);
    expect(chips, findsNWidgets(8));
    final tops = {
      for (final chip in chips.evaluate())
        tester.getRect(find.byWidget(chip.widget)).top,
    };

    expect(tops, hasLength(1));
  });

  testWidgets('status tabs sit below the instrument dropdown', (tester) async {
    await _pumpList(tester, _statusClient(), size: const Size(1280, 800));

    final dropdownBottom = tester
        .getRect(find.byType(AsyncDropdown<InstrumentDto>))
        .bottom;
    final chipsTop = find
        .byType(ChoiceChip)
        .evaluate()
        .map((chip) => tester.getRect(find.byWidget(chip.widget)).top)
        .reduce((a, b) => a < b ? a : b);

    expect(chipsTop, greaterThan(dropdownBottom));
  });

  testWidgets(
    'pending row shows the Akcije column without horizontal scrolling',
    (tester) async {
      const size = Size(1280, 800);
      await _pumpList(
        tester,
        _statusClient(extra: {'rentalStatus': 'Pending'}),
        size: size,
      );

      final screen = Offset.zero & size;
      for (final finder in [find.text('Odobri'), find.text('Odbij')]) {
        final rect = tester.getRect(finder);
        expect(screen.contains(rect.topLeft), isTrue);
        expect(screen.contains(rect.bottomRight), isTrue);
      }
    },
  );

  testWidgets('overdue row has the danger row color', (tester) async {
    await _pumpList(
      tester,
      _statusClient(
        extra: {
          'dueAt': DateTime.now()
              .subtract(const Duration(days: 2))
              .toIso8601String(),
          'overdueDays': 4,
        },
      ),
      size: const Size(1280, 800),
    );

    final rows = tester.widget<Table>(find.byType(Table)).children;
    final colors = [
      for (final row in rows) (row.decoration as BoxDecoration?)?.color,
    ];

    expect(colors, contains(StatusTone.danger.background));
  });

  testWidgets(
    'long instrument name ellipsizes in the Instrument column without overflow',
    (tester) async {
      final model = 'Stratocaster-Deluxe-' * 4;
      await _pumpList(
        tester,
        _statusClient(extra: {'instrumentModel': model}),
        size: const Size(1280, 800),
      );

      expect(tester.takeException(), isNull);

      final label = tester.widget<Text>(find.text(model));
      expect(label.overflow, TextOverflow.ellipsis);

      expect(
        find.byWidgetPredicate(
          (widget) => widget is Tooltip && widget.message == model,
        ),
        findsOneWidget,
      );
    },
  );
}
