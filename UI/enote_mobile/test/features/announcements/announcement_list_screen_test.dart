import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/announcements/announcement_detail_screen.dart';
import 'package:enote_mobile/features/announcements/announcement_list_screen.dart';
import 'package:enote_mobile/features/announcements/announcement_provider.dart';
import 'package:enote_mobile/shell/app_router.dart';

import '../../helpers.dart';

const _announcement53Json = {
  'id': 53,
  'courseId': 12,
  'title': 'Koncert u petak',
  'content': 'Vidimo se u sali 2.',
  'scope': 'Course',
  'courseName': 'Gitara 1',
  'publishedAt': '2026-09-12T10:00:00',
};

const _pageBody = {
  'items': [_announcement53Json],
  'page': 1,
  'pageSize': 20,
  'totalCount': 1,
};

Widget _app(ScriptedClient client) {
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
  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<AnnouncementProvider>(
        create: (_) => AnnouncementProvider(apiClient: apiClient),
      ),
    ],
    child: MaterialApp(
      onGenerateRoute: AppRouter.onGenerateRoute,
      home: const Scaffold(body: AnnouncementListScreen()),
    ),
  );
}

void main() {
  testWidgets('tapping a row opens the detail screen for the announcement id',
      (tester) async {
    final client = ScriptedClient(
      (request) => request.url.path == '/api/v1/student/announcements/53'
          ? jsonResponse(_announcement53Json, 200)
          : jsonResponse(_pageBody, 200),
    );

    await tester.pumpWidget(_app(client));
    await tester.pumpAndSettle();

    expect(find.text('Koncert u petak'), findsOneWidget);

    await tester.tap(find.text('Koncert u petak'));
    await tester.pumpAndSettle();

    final detail = tester.widget<AnnouncementDetailScreen>(
      find.byType(AnnouncementDetailScreen),
    );
    expect(detail.announcementId, 53);
    expect(
      client.requests.map((request) => request.url.path),
      contains('/api/v1/student/announcements/53'),
    );
  });
}
