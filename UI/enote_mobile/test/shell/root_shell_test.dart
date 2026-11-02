import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/announcements/announcement_detail_screen.dart';
import 'package:enote_mobile/features/announcements/announcement_provider.dart';
import 'package:enote_mobile/features/courses/course_detail_screen.dart';
import 'package:enote_mobile/features/courses/course_provider.dart';
import 'package:enote_mobile/features/instruments/instrument_provider.dart';
import 'package:enote_mobile/features/lectures/lecture_provider.dart';
import 'package:enote_mobile/features/rentals/payments/rental_payment_provider.dart';
import 'package:enote_mobile/features/rentals/rental_provider.dart';
import 'package:enote_mobile/features/tuition/tuition_payment_provider.dart';
import 'package:enote_mobile/realtime/notification_hub_client.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/shell/app_router.dart';
import 'package:enote_mobile/shell/root_shell.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../helpers.dart';

class _FakeHub extends NotificationHubClient {
  int startCalls = 0;
  int stopCalls = 0;

  _FakeHub()
    : super(
        hubUrl: 'http://localhost/hubs/notifications',
        tokenProvider: () async => 'token',
      );

  @override
  Future<void> start() async {
    startCalls++;
  }

  @override
  Future<void> stop() async {
    stopCalls++;
  }
}

class _Harness {
  late final ScriptedClient client;
  late final AuthState authState;
  late final ApiClient apiClient;
  late final NotificationController notifications;
  late final SessionController session;
  late final _FakeHub hub;

  _Harness() {
    client = ScriptedClient((request) {
      final path = request.url.path;
      late Object body;
      if (request.method == 'GET' && path == '/api/v1/users/me') {
        body = meJson();
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/notifications/unread-count') {
        body = {'unreadCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/notifications') {
        body = {'items': [], 'totalCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/instruments') {
        body = {'items': [], 'totalCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/rentals') {
        body = {'items': [], 'totalCount': 0};
      } else if (request.method == 'GET' &&
          path == '/api/v1/student/announcements/53') {
        body = {
          'id': 53,
          'courseId': 12,
          'title': 'Koncert u petak',
          'content': 'Vidimo se u sali 2.',
          'scope': 'Course',
          'courseName': 'Gitara 1',
          'publishedAt': '2026-09-12T10:00:00',
        };
      } else {
        body = {'message': 'OK'};
      }
      return jsonResponse(body, 200);
    });
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
    hub = _FakeHub();
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: notifications,
    );
  }

  Widget shellApp() {
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
        ChangeNotifierProvider<AnnouncementProvider>(
          create: (_) => AnnouncementProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
        Provider<TuitionPaymentProvider>(
          create: (_) => TuitionPaymentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
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

void main() {
  testWidgets('disposing the shell stops the hub', (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.shellApp());
    await tester.pumpAndSettle();

    expect(harness.hub.startCalls, 1);

    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pumpAndSettle();

    expect(harness.hub.stopCalls, 1);
  });

  testWidgets('a notification with only courseId opens the course on tab 1',
      (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.shellApp());
    await tester.pumpAndSettle();

    RootShell.routeNotification(NotificationDto(
      id: 1,
      courseId: 47,
      title: 'Nova obavijest na kursu',
      body: 'Obavijest',
      isRead: false,
      createdAt: DateTime.utc(2026, 9, 1, 10),
    ));
    await tester.pumpAndSettle();

    final nav = tester.widget<NavigationBar>(find.byType(NavigationBar));
    expect(nav.selectedIndex, 1);
    final screen =
        tester.widget<CourseDetailScreen>(find.byType(CourseDetailScreen));
    expect(screen.courseId, 47);
  });

  testWidgets('a notification with only announcementId opens the announcement on tab 1',
      (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.shellApp());
    await tester.pumpAndSettle();

    RootShell.routeNotification(NotificationDto(
      id: 2,
      announcementId: 53,
      title: 'Nova objava',
      body: 'Objava',
      isRead: false,
      createdAt: DateTime.utc(2026, 9, 1, 10),
    ));
    await tester.pumpAndSettle();

    final nav = tester.widget<NavigationBar>(find.byType(NavigationBar));
    expect(nav.selectedIndex, 1);
    final screen = tester.widget<AnnouncementDetailScreen>(
      find.byType(AnnouncementDetailScreen),
    );
    expect(screen.announcementId, 53);
  });

  testWidgets('an announcement notification wins over its courseId',
      (tester) async {
    final harness = _Harness();
    await tester.pumpWidget(harness.shellApp());
    await tester.pumpAndSettle();

    RootShell.routeNotification(NotificationDto(
      id: 3,
      announcementId: 53,
      courseId: 47,
      title: 'Nova objava na kursu',
      body: 'Objava',
      isRead: false,
      createdAt: DateTime.utc(2026, 9, 1, 10),
    ));
    await tester.pumpAndSettle();

    final nav = tester.widget<NavigationBar>(find.byType(NavigationBar));
    expect(nav.selectedIndex, 1);
    final screen = tester.widget<AnnouncementDetailScreen>(
      find.byType(AnnouncementDetailScreen),
    );
    expect(screen.announcementId, 53);
    expect(find.byType(CourseDetailScreen), findsNothing);
  });
}
