import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/instrument_detail_screen.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';
const _instrumentId = 3;

const _debtCopy =
    'Imate neizmireno dugovanje od prethodnog iznajmljivanja. '
    'Izmirite ga prije novog zahtjeva.';

/// Routes by path so one client can serve the screen's whole first load.
class _DetailStubClient extends http.BaseClient {
  final bool isAvailable;
  final bool hasDebt;
  final int? debtRentalId;
  final DateTime? membershipPaidUntil;
  final int createStatus;
  final String createBody;

  final List<http.BaseRequest> sent = [];

  _DetailStubClient({
    this.isAvailable = true,
    this.hasDebt = false,
    this.debtRentalId,
    this.membershipPaidUntil,
    this.createStatus = 201,
    this.createBody = '{}',
  });

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    sent.add(request);
    final path = request.url.path;
    String body;
    int status = 200;
    if (path.endsWith('/instruments/public/$_instrumentId')) {
      body = jsonEncode({
        'id': _instrumentId,
        'model': 'C40',
        'manufacturer': 'Yamaha',
        'description': 'Klasična gitara za početnike.',
        'instrumentTypeId': 1,
        'instrumentType': 'Klasična gitara',
        'musicStore': 'Muzika d.o.o.',
        'isAvailable': isAvailable,
      });
    } else if (path.endsWith('/student/rentals/debt')) {
      body = jsonEncode({
        'hasUnpaidDebt': hasDebt,
        if (debtRentalId != null) 'rentalId': debtRentalId,
      });
    } else if (path.endsWith('/users/me')) {
      body = jsonEncode({
        'role': 'Student',
        'username': 'student',
        'email': 'student@enote.com',
        'profile': {
          'id': 1,
          'membershipPaidUntil': membershipPaidUntil?.toIso8601String(),
        },
        'hasPicture': false,
      });
    } else if (path.endsWith('/student/rentals')) {
      status = createStatus;
      body = createBody;
    } else {
      status = 204;
      body = '';
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      status,
      headers: {'content-type': 'application/json'},
    );
  }
}

class _Harness {
  final _DetailStubClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final SessionController session;
  final List<RouteSettings> pushed = [];

  _Harness(this.client) {
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
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: NotificationController(
        apiClient: apiClient,
        endpoint: 'student/notifications',
      ),
    );
  }

  Widget app() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<SessionController>.value(value: session),
        ChangeNotifierProvider<InstrumentProvider>(
          create: (_) => InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.dark,
        onGenerateRoute: (settings) {
          pushed.add(settings);
          return AppRouter.onGenerateRoute(settings);
        },
        home: const InstrumentDetailScreen(instrumentId: _instrumentId),
      ),
    );
  }
}

Future<_Harness> _pumpDetail(
  WidgetTester tester,
  _DetailStubClient client,
) async {
  // Tall surface: the 16:9 hero pushes the request button past the default
  // 800x600 test viewport, and every assertion here is about that button.
  tester.view.physicalSize = const Size(400, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);

  final harness = _Harness(client);
  await harness.session.reloadProfile();
  await tester.pumpWidget(harness.app());
  await tester.pumpAndSettle();
  return harness;
}

DateTime _future() => DateTime.now().add(const Duration(days: 365));
DateTime _past() => DateTime(2020, 3, 4);

bool _requestButtonEnabled(WidgetTester tester) {
  final button = tester.widget<FilledButton>(
    find.widgetWithText(FilledButton, 'Zatraži iznajmljivanje'),
  );
  return button.onPressed != null;
}

void main() {
  testWidgets('no blocking reason leaves the request button enabled', (
    tester,
  ) async {
    await _pumpDetail(
      tester,
      _DetailStubClient(membershipPaidUntil: _future()),
    );

    expect(find.text('Yamaha C40'), findsOneWidget);
    expect(find.text('Yamaha · Klasična gitara'), findsOneWidget);
    expect(find.text('Muzika d.o.o.'), findsOneWidget);
    expect(find.text('Dostupno'), findsOneWidget);
    expect(_requestButtonEnabled(tester), isTrue);
    expect(find.text(_debtCopy), findsNothing);
  });

  testWidgets('reason 1: unpaid debt wins over every other reason', (
    tester,
  ) async {
    // All three client-side reasons apply at once.
    await _pumpDetail(
      tester,
      _DetailStubClient(
        isAvailable: false,
        hasDebt: true,
        debtRentalId: 9,
        membershipPaidUntil: _past(),
      ),
    );

    expect(find.text(_debtCopy), findsOneWidget);
    expect(find.text('Plati sada'), findsOneWidget);
    expect(find.textContaining('Članarina'), findsNothing);
    expect(find.text('Instrument trenutno nije dostupan.'), findsNothing);
    expect(_requestButtonEnabled(tester), isFalse);
  });

  testWidgets('reason 2: expired membership wins over unavailability', (
    tester,
  ) async {
    await _pumpDetail(
      tester,
      _DetailStubClient(isAvailable: false, membershipPaidUntil: _past()),
    );

    expect(
      find.text(
        'Članarina je istekla ${formatDate(_past())} '
        'Obratite se školi za obnovu.',
      ),
      findsOneWidget,
    );
    expect(find.text('Instrument trenutno nije dostupan.'), findsNothing);
    expect(find.text('Plati sada'), findsNothing);
    expect(_requestButtonEnabled(tester), isFalse);
  });

  testWidgets('reason 2 with no membership date at all', (tester) async {
    await _pumpDetail(tester, _DetailStubClient());

    expect(find.text('Članarina nije aktivna.'), findsOneWidget);
    expect(_requestButtonEnabled(tester), isFalse);
  });

  testWidgets('reason 3: an unavailable instrument blocks the request', (
    tester,
  ) async {
    await _pumpDetail(
      tester,
      _DetailStubClient(isAvailable: false, membershipPaidUntil: _future()),
    );

    expect(find.text('Instrument trenutno nije dostupan.'), findsOneWidget);
    expect(find.text('Nedostupno'), findsOneWidget);
    expect(_requestButtonEnabled(tester), isFalse);
  });

  testWidgets('Plati sada pushes the payment route with the debt rental id', (
    tester,
  ) async {
    final harness = await _pumpDetail(
      tester,
      _DetailStubClient(
        hasDebt: true,
        debtRentalId: 9,
        membershipPaidUntil: _future(),
      ),
    );

    await tester.tap(find.text('Plati sada'));
    await tester.pumpAndSettle();

    final route = harness.pushed.last;
    expect(route.name, AppRouter.payment);
    expect((route.arguments as PaymentArgs).rentalId, 9);
  });

  testWidgets('reason 4: the server refusal renders under the sheet button', (
    tester,
  ) async {
    await _pumpDetail(
      tester,
      _DetailStubClient(
        membershipPaidUntil: _future(),
        createStatus: 400,
        createBody: jsonEncode({
          'message': 'Već imate zahtjev na čekanju za ovaj instrument.',
        }),
      ),
    );

    await tester.tap(find.text('Zatraži iznajmljivanje'));
    await tester.pumpAndSettle();
    expect(find.text('Pošalji zahtjev'), findsOneWidget);

    await tester.tap(find.text('Pošalji zahtjev'));
    await tester.pumpAndSettle();
    expect(find.text('Poslati zahtjev za Yamaha C40?'), findsOneWidget);

    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(
      find.text('Već imate zahtjev na čekanju za ovaj instrument.'),
      findsOneWidget,
    );
    expect(find.text('Pošalji zahtjev'), findsOneWidget);
  });

  testWidgets('opening the screen records a view without blocking it', (
    tester,
  ) async {
    final client = _DetailStubClient(membershipPaidUntil: _future());
    await _pumpDetail(tester, client);

    final view = client.sent.where(
      (r) =>
          r.method == 'POST' &&
          r.url.path.endsWith('/student/instruments/$_instrumentId/view'),
    );
    expect(view, hasLength(1));
    expect(find.text('Yamaha C40'), findsOneWidget);
  });
}
