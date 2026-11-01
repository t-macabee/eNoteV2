import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_detail_dialog.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_form_screen.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_list_screen.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_provider.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

import '../../../helpers.dart';

void main() {
  Future<void> pumpAnnouncementList(WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final client = ScriptedClient((request) {
      if (request.url.toString().contains(
        'instructor/courses/1/announcements',
      )) {
        return jsonResponse({
          'items': [
            {
              'id': 1,
              'title': 'Objava A',
              'content': 'Sadržaj A',
              'publishedAt': DateTime(2026, 1, 5).toIso8601String(),
            },
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
        }, 200);
      }
      return jsonResponse('{}', 404);
    });

    final authState = AuthState(
      baseUrl: 'http://localhost/',
      httpClient: client,
      tokenReader: () => fakeJwt(username: 'test', role: 'Instructor'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost/',
      authState: authState,
      httpClient: client,
    );
    final provider = AnnouncementProvider(apiClient: apiClient, courseId: 1);

    await tester.pumpWidget(
      MultiProvider(
        providers: [Provider<ApiClient>.value(value: apiClient)],
        child: MaterialApp(
          home: Scaffold(
            body: AnnouncementListScreen(
              provider: provider,
              presentation: EntityListPresentation.embedded,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('row tap opens the announcement detail dialog', (tester) async {
    await pumpAnnouncementList(tester);

    await tester.tap(find.text('Objava A'));
    await tester.pumpAndSettle();

    expect(find.byType(AnnouncementDetailDialog), findsOneWidget);
    expect(find.byType(AnnouncementFormScreen), findsNothing);
  });

  testWidgets('pencil opens the announcement form', (tester) async {
    await pumpAnnouncementList(tester);

    await tester.tap(find.byTooltip('Uredi').first);
    await tester.pumpAndSettle();

    expect(find.byType(AnnouncementFormScreen), findsOneWidget);
    expect(find.byType(AnnouncementDetailDialog), findsNothing);
  });
}
