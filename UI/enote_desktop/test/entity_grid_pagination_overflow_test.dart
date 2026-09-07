import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

class _NoopHttpClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(
      const Stream.empty(),
      200,
    );
  }
}

void main() {
  testWidgets(
      'EntityGridScreen pagination row does not overflow at wide, uncapped widths',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1400, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: _NoopHttpClient(),
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: EntityGridScreen<String>(
              config: EntityGridConfig<String>(
                fetcher: (page, pageSize, search) async => PagedResult<String>(
                  items: const ['Item 1', 'Item 2'],
                  page: 1,
                  pageSize: 24,
                  totalCount: 2,
                ),
                titleOf: (item) => item,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.textContaining('Stranica'), findsOneWidget);
  });

  testWidgets(
      'EntityGridScreen renders Stranica 1 od 1 on empty result set',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: _NoopHttpClient(),
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: EntityGridScreen<String>(
              config: EntityGridConfig<String>(
                fetcher: (page, pageSize, search) async => PagedResult<String>(
                  items: const [],
                  page: 1,
                  pageSize: 24,
                  totalCount: 0,
                ),
                titleOf: (item) => item,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Stranica 1 od 1'), findsOneWidget);
  });
}
