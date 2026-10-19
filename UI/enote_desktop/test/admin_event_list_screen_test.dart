import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/admin/event/event_form_screen.dart';
import 'package:enote_desktop/features/admin/event/event_list_screen.dart';
import 'package:enote_desktop/features/admin/event/event_provider.dart';
import 'package:enote_desktop/features/admin/instructor/instructor_provider.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
  final url = request.url.toString();
  if (request.method == 'GET') {
    if (url.contains('admin/instructors')) {
      return jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 200,
        'totalCount': 0,
      }, 200);
    }
    if (url.contains('admin/addresses')) {
      return jsonResponse(const {
        'items': [],
        'page': 1,
        'pageSize': 200,
        'totalCount': 0,
      }, 200);
    }
    if (url.contains('admin/events')) {
      return jsonResponse({
        'items': [
          {
            'id': 1,
            'title': 'Platform Gala',
            'description': 'Gala concert across all departments',
            'startsAt': '2026-09-10T18:00:00Z',
            'endsAt': '2026-09-10T20:00:00Z',
            'addressId': null,
            'courseId': null,
            'instructorId': null,
          },
          {
            'id': 2,
            'title': 'Guitar Masterclass',
            'description': 'Masterclass scoped to course and instructor',
            'startsAt': '2026-09-12T14:00:00Z',
            'endsAt': '2026-09-12T16:00:00Z',
            'addressId': null,
            'courseId': 10,
            'courseName': 'Classical Guitar I',
            'instructorId': 5,
          },
        ],
        'page': 1,
        'pageSize': 24,
        'totalCount': 2,
      }, 200);
    }
  }
  return jsonResponse(const {}, 200);
});

Widget _buildTestApp(ScriptedClient mockClient) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    httpClient: mockClient,
    tokenReader: () => fakeJwt(),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: mockClient,
  );

  return MultiProvider(
    providers: [
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<AuthState>.value(value: authState),
      ChangeNotifierProvider<InstructorProvider>(
        create: (_) => InstructorProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<EventProvider>(
        create: (_) => EventProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<AddressProvider>(
        create: (_) => AddressProvider(apiClient: apiClient),
      ),
    ],
    child: const MaterialApp(home: EventListScreen()),
  );
}

void main() {
  testWidgets(
    'the event toolbar fits a 1280 x 720 window beside the 281 px sidebar',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280 - 281, 720);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(_buildTestApp(_client()));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      final search = find.widgetWithText(TextField, 'Pretraži po nazivu...');
      expect(tester.getSize(search).width, greaterThanOrEqualTo(200));
    },
  );

  testWidgets(
    'EventListScreen displays both platform-wide and scoped events with read-only badge on scoped events',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _client();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      expect(find.text('Platform Gala'), findsOneWidget);
      expect(find.text('Guitar Masterclass'), findsOneWidget);

      expect(find.text('Samo pregled'), findsOneWidget);
    },
  );

  testWidgets(
    'the admin event grid passes each event imagePath',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _client();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      final grid = tester.widget<EntityGridScreen<EventDto>>(
        find.byType(EntityGridScreen<EventDto>),
      );
      final event = EventDto(
        id: 3,
        title: 'Orchestra Night',
        description: 'Default image',
        startsAt: DateTime.utc(2026, 9, 20),
        imagePath: '/api/v1/uploads/events/default.webp',
      );
      expect(grid.config.imageUrlOf!(event), event.imagePath);
    },
  );

  testWidgets(
    'Tapping a scoped event opens a read-only EventFormScreen with disabled save and delete',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _client();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Guitar Masterclass'));
      await tester.pumpAndSettle();

      expect(find.byType(EventFormScreen), findsOneWidget);
      expect(find.text('Pregled događaja'), findsOneWidget);

      final saveButton =
          tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Sačuvaj'));
      expect(saveButton.onPressed, isNull);

      final deleteButton =
          tester.widget<OutlinedButton>(find.widgetWithText(OutlinedButton, 'Obriši'));
      expect(deleteButton.onPressed, isNull);

      const scopedReason =
          'Događaj pripada kursu. Administrator može mijenjati i brisati samo događaje na nivou platforme.';
      expect(find.byTooltip(scopedReason), findsOneWidget);
      expect(find.text(scopedReason), findsOneWidget);
    },
  );

  testWidgets(
    'Tapping a platform-wide event opens EventFormScreen dialog for editing',
    (WidgetTester tester) async {
      tester.view.physicalSize = const Size(1400, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final mockClient = _client();
      await tester.pumpWidget(_buildTestApp(mockClient));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Platform Gala'));
      await tester.pumpAndSettle();

      expect(find.byType(EventFormScreen), findsOneWidget);
      expect(find.text('Uredi događaj'), findsOneWidget);
    },
  );
}
