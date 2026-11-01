import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/event/event_form_screen.dart';
import 'package:enote_desktop/features/admin/event/event_provider.dart';

import '../../../helpers.dart';

Widget _app(ApiClient apiClient, Widget form) {
  return MultiProvider(
    providers: [
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<EventProvider>(
        create: (_) => EventProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<AddressProvider>(
        create: (_) => AddressProvider(apiClient: apiClient),
      ),
    ],
    child: MaterialApp(home: form),
  );
}

ApiClient _apiClient() {
  return ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
    httpClient: ScriptedClient(
      (_) => jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 100,
        'totalCount': 0,
      }, 200),
    ),
  );
}

void _useDesktopSurface(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  testWidgets(
    'event add form shows the Osnovni podaci and Termin i lokacija sections',
    (WidgetTester tester) async {
      _useDesktopSurface(tester);
      final apiClient = _apiClient();

      await tester.pumpWidget(_app(apiClient, const EventFormScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Osnovni podaci'), findsOneWidget);
      expect(find.text('Termin i lokacija'), findsOneWidget);
    },
  );

  testWidgets('event edit form shows both sections and the delete button', (
    WidgetTester tester,
  ) async {
    _useDesktopSurface(tester);
    final apiClient = _apiClient();
    final existing = EventDto(
      id: 1,
      title: 'Platform Gala',
      description: 'Gala concert',
      startsAt: DateTime(2026, 9, 10, 18),
      endsAt: DateTime(2026, 9, 10, 20),
    );

    await tester.pumpWidget(
      _app(apiClient, EventFormScreen(existing: existing)),
    );
    await tester.pumpAndSettle();

    expect(find.text('Osnovni podaci'), findsOneWidget);
    expect(find.text('Termin i lokacija'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Obriši'), findsOneWidget);
  });
}
