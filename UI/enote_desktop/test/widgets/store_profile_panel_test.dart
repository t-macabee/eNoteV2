import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/store_profile_panel.dart';

void main() {
  Widget wrap(MusicStoreDto store, {bool compact = false}) {
    final apiClient = ApiClient(
      baseUrl: 'http://localhost:5059/api/v1/',
      authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
    );
    return Provider<ApiClient>.value(
      value: apiClient,
      child: MaterialApp(
        home: Scaffold(
          body: StoreProfilePanel(store: store, compact: compact),
        ),
      ),
    );
  }

  testWidgets('shows dashes for empty values', (tester) async {
    final store = MusicStoreDto(id: 1, storeName: 'Prazna', businessHours: '');
    await tester.pumpWidget(wrap(store));
    await tester.pumpAndSettle();
    expect(find.text('-', skipOffstage: false), findsNWidgets(3));
  });

  testWidgets('shows address, phone and hours', (tester) async {
    final store = MusicStoreDto(
      id: 1,
      storeName: 'Muzička Kuća Sarajevo',
      businessHours: '09:00 - 19:00',
      phoneNumber: '+387 33 555 777',
      addressStreet: 'Titova 10',
      addressCity: 'Sarajevo',
    );
    await tester.pumpWidget(wrap(store));
    await tester.pumpAndSettle();
    expect(
      find.text('Titova 10, Sarajevo', skipOffstage: false),
      findsOneWidget,
    );
    expect(find.text('+387 33 555 777', skipOffstage: false), findsOneWidget);
    expect(find.text('09:00 - 19:00', skipOffstage: false), findsOneWidget);
  });

  testWidgets('compact hides the image', (tester) async {
    final store = MusicStoreDto(
      id: 1,
      storeName: 'Muzička Kuća Sarajevo',
      businessHours: '09:00 - 19:00',
      imagePath: '/uploads/stores/store1.jpg',
    );
    await tester.pumpWidget(wrap(store, compact: true));
    await tester.pumpAndSettle();
    expect(find.byType(AspectRatio), findsNothing);

    await tester.pumpWidget(wrap(store, compact: false));
    await tester.pumpAndSettle();
    expect(find.byType(AspectRatio), findsOneWidget);
  });
}
