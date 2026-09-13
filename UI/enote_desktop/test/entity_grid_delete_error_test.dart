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
  testWidgets('EntityGridScreen surfaces failed delete via ErrorBanner and reloads',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: _NoopHttpClient(),
    );

    int fetchCount = 0;

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: EntityGridScreen<String>(
              config: EntityGridConfig<String>(
                title: 'Stavke',
                fetcher: (page, pageSize, search) async {
                  fetchCount++;
                  return PagedResult<String>(
                    items: const ['Item 1', 'Item 2'],
                    page: 1,
                    pageSize: 24,
                    totalCount: 2,
                  );
                },
                titleOf: (item) => item,
                onDelete: (context, item) async {
                  throw ApiException('Brisanje stavke nije dozvoljeno.');
                },
              ),
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();
    expect(fetchCount, 1);

    final gridState = tester.state<EntityGridScreenState<String>>(
      find.byType(EntityGridScreen<String>),
    );
    gridState.deleteItem('Item 1');
    await tester.pumpAndSettle();

    expect(find.text('Potvrdite brisanje'), findsOneWidget);
    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(find.text('Brisanje stavke nije dozvoljeno.'), findsOneWidget);
    expect(fetchCount, 2);
    expect(tester.takeException(), isNull);
  });
}
