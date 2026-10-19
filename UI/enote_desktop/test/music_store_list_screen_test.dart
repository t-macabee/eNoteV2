import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_list_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
  final url = request.url.toString();

  if (url.contains('admin/cities')) {
    return jsonResponse(const {
      'items': [],
      'page': 1,
      'pageSize': 100,
      'totalCount': 0,
    }, 200);
  }

  if (url.contains('admin/music-stores')) {
    return jsonResponse(const {
      'items': [
        {
          'id': 1,
          'storeName': 'Muzička Kuća',
          'businessHours': '09:00 - 19:00',
          'imagePath': null,
          'addressCity': 'Sarajevo',
        },
      ],
      'page': 1,
      'pageSize': 24,
      'totalCount': 1,
    }, 200);
  }

  return jsonResponse(const {
    'items': [],
    'page': 1,
    'pageSize': 20,
    'totalCount': 0,
  }, 200);
});

void main() {
  testWidgets('the store grid shows the placeholder for a null image and '
      'passes imagePath when one is set', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
      httpClient: _client(),
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          ChangeNotifierProvider<MusicStoreProvider>.value(
            value: MusicStoreProvider(apiClient: apiClient),
          ),
          ChangeNotifierProvider<CityProvider>.value(
            value: CityProvider(apiClient: apiClient),
          ),
        ],
        child: const MaterialApp(home: MusicStoreListScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Muzička Kuća'), findsOneWidget);
    expect(find.byIcon(Icons.storefront_outlined), findsOneWidget);

    final grid = tester.widget<EntityGridScreen<MusicStoreDto>>(
      find.byType(EntityGridScreen<MusicStoreDto>),
    );
    final withImage = MusicStoreDto(
      id: 2,
      storeName: 'Muzička Kuća 2',
      businessHours: '09:00 - 19:00',
      imagePath: '/uploads/stores/photo.jpg',
    );
    expect(grid.config.imageUrlOf!(withImage), '/uploads/stores/photo.jpg');
  });
}
