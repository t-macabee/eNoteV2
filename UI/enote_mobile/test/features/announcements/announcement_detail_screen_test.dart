import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/announcements/announcement_detail_screen.dart';
import 'package:enote_mobile/features/announcements/announcement_provider.dart';
import 'package:enote_mobile/widgets/async_state_view.dart';

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
    child: const MaterialApp(
      home: AnnouncementDetailScreen(announcementId: 53),
    ),
  );
}

void main() {
  testWidgets('loads the announcement by id and shows it', (tester) async {
    final client = ScriptedClient(
      (_) => jsonResponse(_announcement53Json, 200),
    );

    await tester.pumpWidget(_app(client));
    await tester.pumpAndSettle();

    expect(client.requests, hasLength(1));
    expect(client.requests.single.url.path, '/api/v1/student/announcements/53');
    expect(find.text('Koncert u petak'), findsOneWidget);
    expect(
      find.text(
        'Kurs · Gitara 1 · ${formatDateTime(DateTime(2026, 9, 12, 10))}',
      ),
      findsOneWidget,
    );
  });

  testWidgets('a failed load shows the error and retry reloads', (
    tester,
  ) async {
    var calls = 0;
    final client = ScriptedClient((_) {
      calls++;
      return calls == 1
          ? jsonResponse({'message': 'Obavijest nije pronađena.'}, 404)
          : jsonResponse(_announcement53Json, 200);
    });

    await tester.pumpWidget(_app(client));
    await tester.pumpAndSettle();

    final view = tester.widget<AsyncStateView>(find.byType(AsyncStateView));
    expect(view.error, isNotNull);
    expect(find.text('Obavijest nije pronađena.'), findsOneWidget);
    expect(find.text('Koncert u petak'), findsNothing);

    await tester.tap(find.text('Pokušaj ponovo'));
    await tester.pumpAndSettle();

    expect(client.requests, hasLength(2));
    expect(find.text('Koncert u petak'), findsOneWidget);
  });
}
