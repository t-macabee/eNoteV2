import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_list_screen.dart';
import 'package:enote_desktop/features/admin/music_store/music_store_provider.dart';

import '../../../helpers.dart';

Map<String, Object?> _emptyPage() => const {
  'items': [],
  'page': 1,
  'pageSize': 100,
  'totalCount': 0,
};

void main() {
  late FakeSaveFilePicker picker;

  setUp(() {
    picker = FakeSaveFilePicker();
    FilePicker.platform = picker;
  });

  testWidgets('music store list reports every store under a dated name', (
    tester,
  ) async {
    final client = ScriptedClient((request) {
      final path = request.url.path;
      if (path.endsWith('/admin/music-stores/report')) {
        return jsonResponse('%PDF-1.7', 200);
      }
      return jsonResponse(_emptyPage(), 200);
    });
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(),
      httpClient: client,
    );

    await pumpReportScreen(
      tester,
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
        child: const MaterialApp(home: Scaffold(body: MusicStoreListScreen())),
      ),
    );
    await pumpPastDebounce(tester);

    await tapPdfDownload(tester);

    expect(
      client.getUrls.where((u) => u.endsWith('/admin/music-stores/report')),
      hasLength(1),
    );
    expect(
      picker.savedFileName,
      matches(RegExp(r'^music-stores-report-\d{8}\.pdf$')),
    );
  });
}
