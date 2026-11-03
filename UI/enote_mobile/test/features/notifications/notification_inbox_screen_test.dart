import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/assignments/assignment_provider.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';
import 'package:enote_mobile/features/home/home_provider.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';
import 'package:enote_mobile/features/rentals/payments/rental_payment_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/features/notifications/notification_inbox_screen.dart';
import 'package:enote_mobile/realtime/notification_hub_client.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/shell/routes.dart';
import 'package:enote_mobile/shell/root_shell.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

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

ScriptedClient _client({required List<Map<String, dynamic>> items}) {
  var allRead = false;
  return ScriptedClient((request) {
    final path = request.url.path;
    if (request.method == 'PATCH' &&
        path == '/api/v1/student/notifications/read-all') {
      allRead = true;
    }
    if (request.method == 'GET' && path == '/api/v1/users/me') {
      return jsonResponse(meJson(), 200);
    }
    if (request.method == 'GET' &&
        path == '/api/v1/student/notifications/unread-count') {
      final unread = allRead
          ? 0
          : items.where((item) => item['isRead'] == false).length;
      return jsonResponse({'unreadCount': unread}, 200);
    }
    if (request.method == 'GET' && path == '/api/v1/student/notifications') {
      final page = int.tryParse(request.url.queryParameters['page'] ?? '1');
      if (page != null && page > 1) {
        return jsonResponse({'items': [], 'totalCount': items.length}, 200);
      }
      final rendered = allRead
          ? [
              for (final item in items) {...item, 'isRead': true},
            ]
          : items;
      return jsonResponse({'items': rendered, 'totalCount': items.length}, 200);
    }
    return jsonResponse({'message': 'OK'}, 200);
  });
}

extension on ScriptedClient {
  List<http.Request> where(String method, String path) =>
      requests.where((r) => r.method == method && r.url.path == path).toList();
}

class _Harness {
  late final ScriptedClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final NotificationController notifications;
  late final SessionController session;
  late final NotificationHubClient hub;
  final navigatorKey = GlobalKey<NavigatorState>();

  _Harness({required List<Map<String, dynamic>> items}) {
    client = _client(items: items);
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
      connectionFactory: FakeHubConnection.new,
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
        Provider<RentalPaymentProvider>(
          create: (_) => RentalPaymentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<CourseProvider>(
          create: (_) => CourseProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<AssignmentProvider>(
          create: (_) => AssignmentProvider(apiClient: apiClient),
        ),
        Provider<HomeProvider>(
          create: (_) => HomeProvider(apiClient: apiClient),
        ),
      ],
      child: child,
    );
  }

  Widget inboxApp() {
    return providers(
      MaterialApp(theme: mobileTheme, home: const NotificationInboxScreen()),
    );
  }

  Widget shellApp() {
    return providers(
      MaterialApp(
        navigatorKey: navigatorKey,
        theme: mobileTheme,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: RootShell(
          key: RootShell.shellKey,
          onGenerateRoute: AppRouter.onGenerateRoute,
        ),
      ),
    );
  }
}

Future<void> _settle(WidgetTester tester) => tester.pumpAndSettle();

void _setPhoneSurface(WidgetTester tester) {
  tester.view.physicalSize = const Size(375, 812);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  testWidgets('title is fully visible when there are unread notifications', (
    tester,
  ) async {
    _setPhoneSurface(tester);
    final harness = _Harness(
      items: [
        _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
      ],
    );
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.navigatorKey.currentState!.pushNamed(AppRoutes.notifications);
    await _settle(tester);

    final title = find.text('Obavještenja');
    expect(title, findsOneWidget);

    final titleRect = tester.getRect(title);
    final appBarRect = tester.getRect(find.byType(AppBar));
    expect(appBarRect.contains(titleRect.topLeft), isTrue);
    expect(appBarRect.contains(titleRect.bottomRight), isTrue);

    final paragraph = tester.renderObject<RenderParagraph>(title);
    expect(
      paragraph.size.width,
      greaterThanOrEqualTo(paragraph.getMaxIntrinsicWidth(double.infinity)),
    );
  });

  testWidgets('mark all read button is in the list and calls mark all read', (
    tester,
  ) async {
    _setPhoneSurface(tester);
    final harness = _Harness(
      items: [
        _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
      ],
    );
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    final button = find.text('Označi sve kao pročitano');
    expect(button, findsOneWidget);
    expect(
      find.descendant(of: find.byType(AppBar), matching: button),
      findsNothing,
    );
    expect(
      find.descendant(of: find.byType(ListView), matching: button),
      findsOneWidget,
    );

    await tester.tap(button);
    await _settle(tester);

    expect(
      harness.client.where('PATCH', '/api/v1/student/notifications/read-all'),
      hasLength(1),
    );
  });

  testWidgets('mark all read button is hidden when nothing is unread', (
    tester,
  ) async {
    _setPhoneSurface(tester);
    final harness = _Harness(
      items: [_item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true)],
    );
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    expect(find.text('Označi sve kao pročitano'), findsNothing);
  });

  testWidgets('unread rows are bold with a primary dot; read rows are '
      'normal with a hollow dot', (tester) async {
    final harness = _Harness(
      items: [
        _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
        _item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true),
      ],
    );
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    final unreadTitle = tester.widget<Text>(find.text('Unread rental'));
    expect(unreadTitle.style?.fontWeight, FontWeight.w600);
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
  });

  testWidgets('tap unread PATCHes read then navigates to the rental detail', (
    tester,
  ) async {
    final harness = _Harness(
      items: [
        _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
      ],
    );
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.navigatorKey.currentState!.pushNamed(AppRoutes.notifications);
    await _settle(tester);
    expect(find.text('Obavještenja'), findsOneWidget);

    await tester.tap(find.text('Unread rental'));
    await _settle(tester);

    expect(
      harness.client.where('PATCH', '/api/v1/student/notifications/1/read'),
      hasLength(1),
    );
    expect(find.text('Obavještenja'), findsNothing);
    expect(find.text('Iznajmljivanje'), findsWidgets);
  });

  testWidgets('tap read navigates without PATCH', (tester) async {
    final harness = _Harness(
      items: [_item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true)],
    );
    await tester.pumpWidget(harness.shellApp());
    await _settle(tester);

    harness.navigatorKey.currentState!.pushNamed(AppRoutes.notifications);
    await _settle(tester);

    await tester.tap(find.text('Read lecture'));
    await _settle(tester);

    expect(
      harness.client.where('PATCH', '/api/v1/student/notifications/2/read'),
      isEmpty,
    );
    expect(find.text('Obavještenja'), findsNothing);
    expect(find.text('Predavanje'), findsWidgets);
  });

  testWidgets('mark all PATCHes read-all and then hides the action', (
    tester,
  ) async {
    final harness = _Harness(
      items: [
        _item(id: 1, rentalId: 11, title: 'Unread rental', isRead: false),
      ],
    );
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    await tester.tap(find.text('Označi sve kao pročitano'));
    await _settle(tester);

    expect(
      harness.client.where('PATCH', '/api/v1/student/notifications/read-all'),
      hasLength(1),
    );
    expect(find.text('Označi sve kao pročitano'), findsNothing);
  });

  testWidgets('mark all is hidden when nothing is unread', (tester) async {
    final harness = _Harness(
      items: [_item(id: 2, lectureId: 22, title: 'Read lecture', isRead: true)],
    );
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    expect(find.text('Označi sve kao pročitano'), findsNothing);
    expect(
      harness.client.where('PATCH', '/api/v1/student/notifications/read-all'),
      isEmpty,
    );
  });

  testWidgets('scrolling to the end loads the next page of notifications', (
    tester,
  ) async {
    final items = List.generate(
      50,
      (i) => _item(id: i + 1, title: 'Item ${i + 1}', isRead: true),
    );
    final harness = _Harness(items: items);
    await tester.pumpWidget(harness.inboxApp());
    await _settle(tester);

    final pageTwoBefore = harness.client.requests.where(
      (r) => r.url.queryParameters['page'] == '2',
    );
    expect(pageTwoBefore, isEmpty);

    await tester.drag(find.byType(ListView), const Offset(0, -20000));
    await _settle(tester);

    final pageTwos = harness.client.requests.where(
      (r) =>
          r.method == 'GET' &&
          r.url.path == '/api/v1/student/notifications' &&
          r.url.queryParameters['page'] == '2',
    );
    expect(pageTwos, hasLength(1));
    expect(find.text('Učitavanje nije uspjelo.'), findsNothing);
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
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 750));

      expect(find.text('Nova obavijest'), findsOneWidget);
      final snackBar = tester.widget<SnackBar>(find.byType(SnackBar));
      expect(snackBar.persist, isFalse);
      expect(snackBar.behavior, SnackBarBehavior.floating);
      expect(snackBar.duration, const Duration(seconds: 4));

      await tester.pump(const Duration(seconds: 4));
      await tester.pumpAndSettle();
      expect(find.text('Nova obavijest'), findsNothing);
    },
  );

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
    },
  );
}
