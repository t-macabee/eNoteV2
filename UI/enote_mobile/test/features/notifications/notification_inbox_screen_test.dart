import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/payments/payment_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/features/notifications/notification_inbox_screen.dart';
import 'package:enote_mobile/realtime/notification_hub_client.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/shell/root_shell.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

Map<String, dynamic> _meJson() => {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {
    'id': 7,
    'firstName': 'Student',
    'lastName': 'Enote',
    'dateOfBirth': '2001-05-12T00:00:00',
    'membershipPaidUntil': '2027-09-09T00:00:00',
  },
  'hasPicture': false,
};

Map<String, dynamic> _item({
  required int id,
  int? rentalId,
  int? lectureId,
  int? submissionId,
  required String title,
  required bool isRead,
}) {
  final map = <String, dynamic>{
    'id': id,
    'title': title,
    'body': 'Body of $title',
    'isRead': isRead,
    'createdAt': '2026-09-09T14:02:00',
  };
  if (rentalId != null) map['rentalId'] = rentalId;
  if (lectureId != null) map['lectureId'] = lectureId;
  if (submissionId != null) map['submissionId'] = submissionId;
  return map;
}

class _NotificationStubClient extends http.BaseClient {
  final List<http.Request> requests = [];
  final List<Map<String, dynamic>> items;
  bool allRead = false;

  _NotificationStubClient({required this.items});

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request is http.Request) {
      requests.add(request);
      if (request.method == 'PATCH' &&
          request.url.path == '/api/v1/student/notifications/read-all') {
        allRead = true;
      }
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(_bodyFor(request)))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }

  Map<String, dynamic> _bodyFor(http.BaseRequest request) {
    final path = request.url.path;
    if (request.method == 'GET' && path == '/api/v1/users/me') {
      return _meJson();
    }
    if (request.method == 'GET' &&
        path == '/api/v1/student/notifications/unread-count') {
      return {'unreadCount': 0};
    }
    if (request.method == 'GET' && path == '/api/v1/student/notifications') {
      final page = int.tryParse(request.url.queryParameters['page'] ?? '1');
      if (page != null && page > 1) {
        return {'items': [], 'totalCount': items.length};
      }
      final rendered = allRead
          ? [for (final item in items) {...item, 'isRead': true}]
          : items;
      return {'items': rendered, 'totalCount': items.length};
    }
    return {'message': 'OK'};
  }

  List<http.Request> where(String method, String path) => requests
      .where((r) => r.method == method && r.url.path == path)
      .toList();
}

class _Harness {
  late final _NotificationStubClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final NotificationController notifications;
  late final SessionController session;
  late final NotificationHubClient hub;
  final navigatorKey = GlobalKey<NavigatorState>();

  _Harness({required List<Map<String, dynamic>> items}) {
    client = _NotificationStubClient(items: items);
    authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      tokenWriter: (_) {},
      httpClient: client,
    );
    apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
    notifications = NotificationController(
      apiClient: apiClient,
      endpoint: 'student/notifications',
    );
    hub = NotificationHubClient(
      hubUrl: NotificationHubClient.deriveHubUrl(
        'http://10.0.2.2:5059/api/v1/',
      ),
      tokenProvider: () async => 'token',
    );
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: notifications,
    );
  }

  Widget providers(Widget child) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<NotificationController>.value(
          value: notifications,
        ),
        Provider<NotificationHubClient>.value(value: hub),
        ChangeNotifierProvider<SessionController>.value(value: session),
        ChangeNotifierProvider<InstrumentProvider>(
          create: (_) => InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
        Provider<PaymentProvider>(
          create: (_) => PaymentProvider(apiClient: apiClient),
        ),
      ],
      child: child,
    );
  }

  Widget inboxApp() {
    return providers(
      MaterialApp(
        theme: AppTheme.dark,
        home: const NotificationInboxScreen(),
      ),
    );
  }

  Widget shellApp() {
    return providers(
      MaterialApp(
        navigatorKey: navigatorKey,
        theme: AppTheme.dark,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: RootShell(key: RootShell.shellKey),
      ),
    );
  }
}

Future<void> _settle(WidgetTester tester) => tester.pumpAndSettle();

void main() {
  testWidgets('unread rows are bold with a primary dot; read rows are '
      'normal with a hollow dot', (tester) async {
    final harness = _Harness(items: [
      _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
      _item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true),
    ]);
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    final unreadTitle = tester.widget<Text>(find.text('Unread rental'));
    expect(unreadTitle.style?.fontWeight, FontWeight.bold);
    final readTitle = tester.widget<Text>(find.text('Read lecture'));
    expect(readTitle.style?.fontWeight, FontWeight.normal);
    expect(readTitle.style?.color, AppTheme.textSecondary);

    expect(
      find.byWidgetPredicate(
        (w) =>
            w is Container &&
            w.decoration is BoxDecoration &&
            (w.decoration as BoxDecoration).color == AppTheme.primary,
      ),
      findsOneWidget,
    );
    expect(find.text('Učitaj još'), findsNothing);
  });

  testWidgets('tap unread PATCHes read then navigates to the rental detail',
      (tester) async {
    final harness = _Harness(items: [
      _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
    ]);
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.navigatorKey.currentState!.pushNamed(AppRouter.notifications);
    await _settle(tester);
    expect(find.text('Obavještenja'), findsOneWidget);

    await tester.tap(find.text('Unread rental'));
    await _settle(tester);

    expect(
      harness.client.where(
        'PATCH',
        '/api/v1/student/notifications/1/read',
      ),
      hasLength(1),
    );
    expect(find.text('Obavještenja'), findsNothing);
    expect(find.text('Iznajmljivanje'), findsWidgets);
  });

  testWidgets('tap read navigates without PATCH', (tester) async {
    final harness = _Harness(items: [
      _item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true),
    ]);
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.navigatorKey.currentState!.pushNamed(AppRouter.notifications);
    await _settle(tester);

    await tester.tap(find.text('Read lecture'));
    await _settle(tester);

    expect(
      harness.client.where(
        'PATCH',
        '/api/v1/student/notifications/2/read',
      ),
      isEmpty,
    );
    expect(find.text('Obavještenja'), findsNothing);
    expect(find.text('Predavanje'), findsWidgets);
  });

  testWidgets('mark all PATCHes read-all and disables with tooltip at 0',
      (tester) async {
    final harness = _Harness(items: [
      _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
    ]);
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    await tester.tap(find.text('Označi sve kao pročitano'));
    await _settle(tester);

    expect(
      harness.client.where(
        'PATCH',
        '/api/v1/student/notifications/read-all',
      ),
      hasLength(1),
    );
    expect(
      find.byWidgetPredicate(
        (w) => w is Tooltip && w.message == 'Nema nepročitanih',
      ),
      findsOneWidget,
    );
    final button = tester.widget<TextButton>(
      find.widgetWithText(TextButton, 'Označi sve kao pročitano'),
    );
    expect(button.onPressed, isNull);
  });

  testWidgets('mark all is disabled with tooltip when nothing is unread',
      (tester) async {
    final harness = _Harness(items: [
      _item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true),
    ]);
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    final button = tester.widget<TextButton>(
      find.widgetWithText(TextButton, 'Označi sve kao pročitano'),
    );
    expect(button.onPressed, isNull);
    expect(
      find.byWidgetPredicate(
        (w) => w is Tooltip && w.message == 'Nema nepročitanih',
      ),
      findsOneWidget,
    );
    expect(
      harness.client.where(
        'PATCH',
        '/api/v1/student/notifications/read-all',
      ),
      isEmpty,
    );
  });

  testWidgets('load more is visible iff hasMore and requests page 2',
      (tester) async {
    final items = List.generate(
      50,
      (i) => _item(id: i + 1, title: 'Item ${i + 1}', isRead: true),
    );
    final harness = _Harness(items: items);
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    await tester.scrollUntilVisible(
      find.text('Učitaj još'),
      2000,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.pumpAndSettle();
    await tester.drag(find.byType(ListView), const Offset(0, -300));
    await tester.pumpAndSettle();
    expect(find.text('Učitaj još'), findsOneWidget);
    await tester.tap(find.text('Učitaj još'));
    await _settle(tester);

    final pageTwos = harness.client.requests.where(
      (r) =>
          r.method == 'GET' &&
          r.url.path == '/api/v1/student/notifications' &&
          r.url.queryParameters['page'] == '2',
    );
    expect(pageTwos, hasLength(1));
    expect(find.text('Učitaj još'), findsNothing);
  });

  testWidgets(
      'push notification snackbar is floating, has persist false, and auto-dismisses',
      (tester) async {
    final harness = _Harness(items: []);
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.hub.onPush(
      NotificationPushDto(
        title: 'Nova obavijest',
        body: 'Body text',
        createdAt: DateTime(2026, 9, 10),
      ),
    );
    // Complete entrance animation and start the display timer
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 750));

    expect(find.text('Nova obavijest'), findsOneWidget);
    final snackBar = tester.widget<SnackBar>(find.byType(SnackBar));
    expect(snackBar.persist, isFalse);
    expect(snackBar.behavior, SnackBarBehavior.floating);
    expect(snackBar.duration, const Duration(seconds: 4));

    // Fast forward past the 4-second display duration and settle exit animation
    await tester.pump(const Duration(seconds: 4));
    await tester.pumpAndSettle();
    expect(find.text('Nova obavijest'), findsNothing);
  });

  testWidgets(
      'consecutive push notifications clear previous snackbars without queuing',
      (tester) async {
    final harness = _Harness(items: []);
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.hub.onPush(
      NotificationPushDto(
        title: 'First push',
        body: 'Body 1',
        createdAt: DateTime(2026, 9, 10),
      ),
    );
    await tester.pump();
    expect(find.text('First push'), findsOneWidget);

    harness.hub.onPush(
      NotificationPushDto(
        title: 'Second push',
        body: 'Body 2',
        createdAt: DateTime(2026, 9, 10),
      ),
    );
    await tester.pump();
    expect(find.text('Second push'), findsOneWidget);
    expect(find.text('First push'), findsNothing);
  });
}

