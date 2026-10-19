import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/entity_list_screen.dart';

import 'helpers.dart';

void main() {
  testWidgets('EntityListScreen surfaces failed delete via ErrorBanner and reloads',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    int fetchCount = 0;

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: EntityListScreen<String>(
              config: EntityListConfig<String>(
                title: 'Stavke',
                fetcher: (page, pageSize, search) async {
                  fetchCount++;
                  return PagedResult<String>(
                    items: const ['Item 1', 'Item 2'],
                    page: 1,
                    pageSize: 10,
                    totalCount: 2,
                  );
                },
                columns: [
                  ColumnSpec(
                    label: 'Naziv',
                    value: (item) => item,
                  ),
                ],
                onDelete: (context, item) async {
                  throw ApiException('Brisanje nije uspjelo.');
                },
              ),
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();
    expect(fetchCount, 1);

    await tester.tap(find.byIcon(Icons.delete).first);
    await tester.pumpAndSettle();

    expect(find.text('Potvrdite brisanje'), findsOneWidget);
    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(find.text('Brisanje nije uspjelo.'), findsOneWidget);
    expect(fetchCount, 2);
    expect(tester.takeException(), isNull);
  });

  testWidgets('EntityListScreen names the item in the confirm and shows the deleted message',
      (WidgetTester tester) async {
    final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: authState,
      httpClient: ScriptedClient((_) => jsonResponse('', 200)),
    );

    await tester.pumpWidget(
      Provider<ApiClient>.value(
        value: apiClient,
        child: MaterialApp(
          home: Scaffold(
            body: EntityListScreen<String>(
              config: EntityListConfig<String>(
                title: 'Gradovi',
                fetcher: (page, pageSize, search) async => PagedResult<String>(
                  items: const ['Mostar'],
                  page: 1,
                  pageSize: 10,
                  totalCount: 1,
                ),
                columns: [
                  ColumnSpec(
                    label: 'Naziv',
                    value: (item) => item,
                  ),
                ],
                deleteConfirmMessageOf: (item) =>
                    'Da li ste sigurni da želite da obrišete grad "$item"?',
                deletedMessage: 'Grad je obrisan.',
                onDelete: (context, item) async => true,
              ),
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();
    await tester.tap(find.byIcon(Icons.delete).first);
    await tester.pumpAndSettle();

    expect(
      find.text('Da li ste sigurni da želite da obrišete grad "Mostar"?'),
      findsOneWidget,
    );
    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(find.text('Grad je obrisan.'), findsOneWidget);
  });
}
