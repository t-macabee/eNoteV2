import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_list_screen.dart';
import 'package:enote_desktop/features/shared/announcement/announcement_provider.dart';
import 'package:enote_desktop/features/shared/announcement/store_announcement_provider.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

import 'helpers.dart';

void main() {
  testWidgets('AnnouncementListScreen renders for course provider', (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    bool fetchCalled = false;

    final client = ScriptedClient((request) {
      if (request.url.toString().contains('instructor/courses/1/announcements')) {
        fetchCalled = true;
        return jsonResponse({
            'items': [
              {
                'id': 1,
                'title': 'Test objava',
                'content': 'Sadržaj',
                'publishedAt': DateTime.now().toIso8601String(),
              }
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

    await tester.pumpWidget(MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
      ],
      child: MaterialApp(
        home: AnnouncementListScreen(
          provider: provider,
          title: 'Objave — Kurs 1',
        ),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Objave — Kurs 1'), findsOneWidget);
    expect(find.text('Dodaj'), findsOneWidget);
    expect(find.text('Test objava'), findsOneWidget);
    expect(fetchCalled, isTrue);
  });

  testWidgets('AnnouncementListScreen renders for store provider', (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    
    bool fetchCalled = false;

    final client = ScriptedClient((request) {
      if (request.url.toString().contains('shop/announcements')) {
        fetchCalled = true;
        return jsonResponse({
            'items': [
              {
                'id': 2,
                'title': 'Store objava',
                'content': 'Sadržaj',
                'publishedAt': DateTime.now().toIso8601String(),
              }
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
      tokenReader: () => fakeJwt(username: 'test', role: 'StoreEmployee'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost/',
      authState: authState,
      httpClient: client,
    );
    final provider = StoreAnnouncementProvider(apiClient: apiClient);

    await tester.pumpWidget(MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: AnnouncementListScreen(
            provider: provider,
            presentation: EntityListPresentation.embedded,
          ),
        ),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.byType(EntityToolbar), findsOneWidget);
    expect(find.text('Dodaj'), findsOneWidget);
    expect(find.text('Store objava'), findsOneWidget);
    expect(fetchCalled, isTrue);
  });

  testWidgets('AnnouncementListScreen search filters on title', (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final queries = <Map<String, String>>[];

    final client = ScriptedClient((request) {
      queries.add(request.url.queryParameters);
      return jsonResponse({
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }, 200);
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

    await tester.pumpWidget(MaterialApp(
      home: AnnouncementListScreen(provider: provider, title: 'Objave'),
    ));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'ispit');
    await pumpPastDebounce(tester);
    await tester.pumpAndSettle();

    expect(queries.any((q) => q['title'] == 'ispit'), isTrue);
  });

  testWidgets('F3-10: cancelling the announcement form does not refresh',
      (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    var fetches = 0;

    final client = ScriptedClient((request) {
      fetches++;
      return jsonResponse({
          'items': [],
          'page': 1,
          'pageSize': 20,
          'totalCount': 0,
        }, 200);
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

    await tester.pumpWidget(MaterialApp(
      home: AnnouncementListScreen(provider: provider, title: 'Objave'),
    ));
    await tester.pumpAndSettle();
    expect(fetches, 1);

    await tester.tap(find.text('Dodaj'));
    await tester.pumpAndSettle();
    expect(find.text('Otkaži'), findsOneWidget);

    await tester.tap(find.text('Otkaži'));
    await tester.pumpAndSettle();

    expect(fetches, 1);
  });

  testWidgets('AnnouncementListScreen renders a thumbnail or a placeholder per row',
      (tester) async {
    tester.view.physicalSize = const Size(1400, 1000);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    const imagePath = '/api/v1/uploads/announcements/harmony.webp';

    final client = ScriptedClient((request) {
      if (request.url.toString().contains('instructor/courses/1/announcements')) {
        return jsonResponse({
          'items': [
            {
              'id': 1,
              'title': 'Objava sa slikom',
              'content': 'Sadržaj',
              'imagePath': imagePath,
              'publishedAt': DateTime(2026, 1, 5).toIso8601String(),
            },
            {
              'id': 2,
              'title': 'Objava bez slike',
              'content': 'Sadržaj',
              'publishedAt': DateTime(2026, 1, 6).toIso8601String(),
            }
          ],
          'page': 1,
          'pageSize': 20,
          'totalCount': 2,
        }, 200);
      }
      return jsonResponse('{}', 404);
    });

    final authState = AuthState(
      baseUrl: 'http://localhost:5059/api/v1/',
      httpClient: client,
      tokenReader: () => fakeJwt(username: 'test', role: 'Instructor'),
    );
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
    final provider = AnnouncementProvider(apiClient: apiClient, courseId: 1);

    await tester.pumpWidget(MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
      ],
      child: MaterialApp(
        home: AnnouncementListScreen(provider: provider, title: 'Objave'),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Slika'), findsOneWidget);

    final thumbnails =
        tester.widgetList<ImageThumbnail>(find.byType(ImageThumbnail)).toList();
    expect(thumbnails, hasLength(2));

    final withImage = thumbnails.singleWhere((t) => t.imageUrl != null);
    expect(withImage.imageUrl, imagePath);

    final withoutImage = thumbnails.singleWhere((t) => t.imageUrl == null);
    expect(
      find.descendant(
        of: find.byWidget(withoutImage),
        matching: find.byIcon(Icons.image),
      ),
      findsOneWidget,
    );
  });
}
