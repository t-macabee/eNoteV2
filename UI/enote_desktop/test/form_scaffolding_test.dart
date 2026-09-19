import 'dart:convert';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/city/city_form_screen.dart';
import 'package:enote_desktop/features/admin/city/city_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'helpers.dart';

void main() {
  testWidgets(
    'city form edit mode pre-fills the field and saves through PUT',
    (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final httpClient = ScriptedClient((request) {
        return jsonResponse(const {'id': 7, 'name': 'Mostar'}, 200);
      });
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: httpClient,
      );

      await tester.pumpWidget(
        ChangeNotifierProvider<CityProvider>.value(
          value: CityProvider(apiClient: apiClient),
          child: MaterialApp(
            home: Scaffold(
              body: Builder(
                builder: (context) => ElevatedButton(
                  onPressed: () => EntityFormScaffold.showAsDialog(
                    context,
                    builder: (_) => CityFormScreen(
                      existing: CityDto(id: 7, name: 'Sarajevo'),
                      presentation: EntityFormPresentation.dialog,
                    ),
                  ),
                  child: const Text('Open'),
                ),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.text('Uredi grad'), findsOneWidget);
      final field = tester.widget<TextFormField>(
        find.widgetWithText(TextFormField, 'Naziv grada'),
      );
      expect(field.controller?.text, 'Sarajevo');

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Naziv grada'),
        'Mostar',
      );
      await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
      await tester.pumpAndSettle();

      expect(
        httpClient.putUrls,
        ['http://localhost:5059/api/v1/admin/cities/7'],
      );
      expect(jsonDecode(httpClient.putBodies.single), {'name': 'Mostar'});
      expect(find.text('Uredi grad'), findsNothing);
    },
  );
}
