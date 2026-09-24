import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_detail_dialog.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_form_screen.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_form_screen.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_screen.dart';
import 'package:enote_desktop/widgets/entity_grid_screen.dart';

import 'helpers.dart';
import 'store_employee_shell.dart';

Widget _buildTestApp({
  required bool isManager,
  required ScriptedClient httpClient,
}) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () => fakeJwt(role: 'StoreEmployee', isManager: isManager),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: httpClient,
  );
  final storeProvider = ShopStoreProvider(apiClient: apiClient);
  final addressProvider = AddressProvider(apiClient: apiClient);
  final instrumentProvider = InstrumentProvider(apiClient: apiClient);
  final instrumentTypeProvider = ShopInstrumentTypeProvider(apiClient: apiClient);

  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<ShopStoreProvider>.value(value: storeProvider),
      ChangeNotifierProvider<AddressProvider>.value(value: addressProvider),
      ChangeNotifierProvider<InstrumentProvider>.value(value: instrumentProvider),
      ChangeNotifierProvider<ShopInstrumentTypeProvider>.value(value: instrumentTypeProvider),
    ],
    child: const MaterialApp(
      home: Scaffold(
        body: ShopStoreScreen(),
      ),
    ),
  );
}

void main() {
  testWidgets('ShopStoreScreen renders read-only left panel for non-managers and instruments grid',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final httpClient = storeEmployeeShellClient();
    await tester.pumpWidget(_buildTestApp(isManager: false, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Verify info rows populated
    expect(find.text('Muzička Prodavnica'), findsOneWidget);
    expect(find.text('08:00 - 16:00'), findsOneWidget);
    expect(find.text('+387 61 111 222'), findsOneWidget);
    expect(find.text('Ferhadija 15, Sarajevo'), findsOneWidget);

    // Verify Uredi button is not present for non-managers
    expect(find.widgetWithText(ElevatedButton, 'Uredi'), findsNothing);

    // Verify instruments grid is rendered on the right panel
    expect(find.byType(EntityGridScreen<InstrumentDto>), findsOneWidget);
    expect(find.text('Stratocaster'), findsOneWidget);
    expect(find.text('Fender'), findsOneWidget);
  });

  testWidgets(
      'ShopStoreScreen does not render Uredi button in left panel for managers',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final httpClient = storeEmployeeShellClient();
    await tester.pumpWidget(_buildTestApp(isManager: true, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Verify Uredi button is not present in ShopStoreScreen left panel
    expect(find.widgetWithText(ElevatedButton, 'Uredi'), findsNothing);

    // Verify instruments grid is rendered
    expect(find.byType(EntityGridScreen<InstrumentDto>), findsOneWidget);
  });

  testWidgets(
      'MasterScreen renders Uredi prodavnicu in RoleMenu for managers and saves changes via dialog',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final httpClient = storeEmployeeShellClient();
    await tester.pumpWidget(buildStoreEmployeeShell(isManager: true, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Open settings popup menu in bottom-left RoleMenu
    await tester.tap(find.byTooltip('Postavke'));
    await tester.pumpAndSettle();

    // Verify Uredi prodavnicu is present in popup menu
    expect(find.text('Uredi prodavnicu'), findsOneWidget);

    // Tap Uredi prodavnicu to open the dialog
    await tester.tap(find.text('Uredi prodavnicu'));
    await tester.pumpAndSettle();

    // Verify ShopStoreFormScreen is displayed as dialog
    expect(find.byType(ShopStoreFormScreen), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Naziv'), findsOneWidget);

    // Edit a field and save
    await tester.enterText(
        find.widgetWithText(TextFormField, 'Naziv'), 'Ažurirana Prodavnica');
    await tester.tap(find.widgetWithText(FilledButton, 'Sačuvaj'));
    await tester.pumpAndSettle();

    // Verify PUT request was made to shop/store
    expect(httpClient.putUrls, hasLength(1));
    expect(httpClient.putUrls.first, endsWith('shop/store'));
    final body = jsonDecode(httpClient.putBodies.first) as Map<String, dynamic>;
    expect(body['storeName'], equals('Ažurirana Prodavnica'));

    // Verify dialog is dismissed
    expect(find.byType(ShopStoreFormScreen), findsNothing);
  });

  testWidgets(
      'MasterScreen does not render Uredi prodavnicu in RoleMenu for non-managers',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final httpClient = storeEmployeeShellClient();
    await tester.pumpWidget(buildStoreEmployeeShell(isManager: false, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Open settings popup menu in bottom-left RoleMenu
    await tester.tap(find.byTooltip('Postavke'));
    await tester.pumpAndSettle();

    // Verify Uredi prodavnicu is NOT present in popup menu
    expect(find.text('Uredi prodavnicu'), findsNothing);
  });

  testWidgets(
      'ShopStoreScreen: tapping an instrument card opens InstrumentDetailDialog, '
      'supports edit, then delete',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });

    final httpClient = storeEmployeeShellClient();
    await tester.pumpWidget(_buildTestApp(isManager: true, httpClient: httpClient));
    await tester.pumpAndSettle();

    // Tap the instrument card in the right-panel grid
    await tester.tap(find.text('Stratocaster'));
    await tester.pumpAndSettle();

    // Verify InstrumentDetailDialog opened with its detail fields. 'Gitara'
    // also appears in the (still-mounted, now-obscured) instrument-type
    // filter dropdown behind the dialog, so scope the search to the dialog.
    expect(find.byType(InstrumentDetailDialog), findsOneWidget);
    expect(
      find.descendant(
        of: find.byType(InstrumentDetailDialog),
        matching: find.text('Gitara'),
      ),
      findsOneWidget,
    );
    expect(find.text('Dostupan'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Uredi'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Obriši'), findsOneWidget);

    // Tap Uredi to open InstrumentFormScreen, then close it
    await tester.tap(find.widgetWithText(OutlinedButton, 'Uredi'));
    await tester.pumpAndSettle();

    expect(find.byType(InstrumentFormScreen), findsOneWidget);

    await tester.tap(
      find.descendant(
        of: find.byType(InstrumentFormScreen),
        matching: find.byTooltip('Zatvori'),
      ),
    );
    await tester.pumpAndSettle();

    // Back in the detail dialog
    expect(find.byType(InstrumentDetailDialog), findsOneWidget);

    // Tap Obriši and confirm deletion
    await tester.tap(find.widgetWithText(OutlinedButton, 'Obriši'));
    await tester.pumpAndSettle();

    expect(find.text('Potvrdite brisanje'), findsOneWidget);
    await tester.tap(find.text('Potvrdi'));
    await tester.pumpAndSettle();

    expect(httpClient.deletedUrls, hasLength(1));
    expect(httpClient.deletedUrls.first, endsWith('shop/instruments/1'));
    expect(find.byType(InstrumentDetailDialog), findsNothing);
  });
}
