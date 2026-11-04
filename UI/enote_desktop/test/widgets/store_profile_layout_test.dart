import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/store_profile_layout.dart';
import 'package:enote_desktop/widgets/store_profile_panel.dart';

MusicStoreDto _store() => MusicStoreDto(
  id: 1,
  storeName: 'Muzička Kuća Sarajevo',
  businessHours: '09:00 - 19:00',
  phoneNumber: '+387 33 555 777',
  addressStreet: 'Titova 10',
  addressCity: 'Sarajevo',
);

Future<void> _pump(
  WidgetTester tester,
  double w,
  double h,
  Widget content,
) async {
  tester.view.physicalSize = Size(w, h);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });

  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(baseUrl: 'http://localhost:5059/api/v1/'),
  );

  await tester.pumpWidget(
    Provider<ApiClient>.value(
      value: apiClient,
      child: MaterialApp(
        home: Scaffold(
          body: SizedBox(
            width: w,
            height: h,
            child: StoreProfileLayout(store: _store(), content: content),
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('wide layout puts the panel left of the content', (tester) async {
    final key = UniqueKey();
    await _pump(tester, 1000, 700, Container(key: key));
    final panel = find.byType(StoreProfilePanel);
    final content = find.byKey(key);
    expect(tester.getSize(panel).width, 280);
    expect(tester.getTopLeft(content).dx, greaterThanOrEqualTo(281));
  });

  testWidgets('narrow layout stacks the panel above the content', (
    tester,
  ) async {
    final key = UniqueKey();
    await _pump(tester, 544, 700, Container(key: key));
    final panel = find.byType(StoreProfilePanel);
    final content = find.byKey(key);
    expect(
      tester.getBottomLeft(panel).dy,
      lessThanOrEqualTo(tester.getTopLeft(content).dy + 0.01),
    );
    expect(tester.getSize(content).width, 544);
  });

  testWidgets('narrow layout shows no image', (tester) async {
    await _pump(tester, 544, 700, const SizedBox.shrink());
    expect(find.byType(AspectRatio), findsNothing);
    await _pump(tester, 1000, 700, const SizedBox.shrink());
    expect(find.byType(AspectRatio), findsOneWidget);
  });

  testWidgets('no overflow at 544x700 and at 1000x700', (tester) async {
    for (final size in [const Size(544, 700), const Size(1000, 700)]) {
      await _pump(
        tester,
        size.width,
        size.height,
        ListView(
          children: List.generate(30, (i) => ListTile(title: Text('row $i'))),
        ),
      );
      expect(tester.takeException(), isNull);
    }
  });

  testWidgets('layout stacks at 719 px', (tester) async {
    final key = UniqueKey();
    await _pump(tester, 719, 700, Container(key: key));
    final panel = find.byType(StoreProfilePanel);
    final content = find.byKey(key);
    expect(
      tester.getBottomLeft(panel).dy,
      lessThanOrEqualTo(tester.getTopLeft(content).dy + 0.01),
    );
  });

  testWidgets('layout is side by side at 720 px', (tester) async {
    final key = UniqueKey();
    await _pump(tester, 720, 700, Container(key: key));
    final content = find.byKey(key);
    expect(tester.getTopLeft(content).dx, greaterThanOrEqualTo(281));
  });
}
