import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/features/store_employee/employee/shop_employee_list_screen.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_screen.dart';
import 'package:enote_desktop/shell/role_menu.dart';

import 'store_employee_shell.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });
}

Finder _menuEntry(String label) => find.descendant(
      of: find.byType(RoleMenu),
      matching: find.text(label),
    );

Finder _backButton() => find.ancestor(
      of: find.byType(BackButtonIcon),
      matching: find.byType(IconButton),
    );

void main() {
  testWidgets('Back is disabled on the start screen and explains why',
      (WidgetTester tester) async {
    _useDesktopView(tester);
    await tester.pumpWidget(buildStoreEmployeeShell(
      isManager: false,
      httpClient: storeEmployeeShellClient(),
    ));
    await tester.pumpAndSettle();

    expect(tester.widget<IconButton>(_backButton()).onPressed, isNull);
    expect(find.byTooltip('Ovo je početni ekran'), findsOneWidget);
  });

  testWidgets('Back returns to the start screen, not the previous one',
      (WidgetTester tester) async {
    _useDesktopView(tester);
    await tester.pumpWidget(buildStoreEmployeeShell(
      isManager: false,
      httpClient: storeEmployeeShellClient(),
    ));
    await tester.pumpAndSettle();

    await tester.tap(_menuEntry('Zaposlenici'));
    await tester.pumpAndSettle();
    expect(find.byType(ShopEmployeeListScreen), findsOneWidget);

    await tester.tap(_menuEntry('Zahtjevi'));
    await tester.pumpAndSettle();
    expect(find.byType(ShopEmployeeListScreen), findsNothing);

    await tester.tap(_backButton());
    await tester.pumpAndSettle();
    expect(find.byType(ShopStoreScreen), findsOneWidget);
    expect(find.byType(ShopEmployeeListScreen), findsNothing);
    expect(tester.widget<IconButton>(_backButton()).onPressed, isNull);
  });

  testWidgets('Back is disabled again after returning to the start entry from the menu',
      (WidgetTester tester) async {
    _useDesktopView(tester);
    await tester.pumpWidget(buildStoreEmployeeShell(
      isManager: false,
      httpClient: storeEmployeeShellClient(),
    ));
    await tester.pumpAndSettle();

    await tester.tap(_menuEntry('Zaposlenici'));
    await tester.pumpAndSettle();
    expect(tester.widget<IconButton>(_backButton()).onPressed, isNotNull);

    await tester.tap(_menuEntry('Moja prodavnica'));
    await tester.pumpAndSettle();

    expect(tester.widget<IconButton>(_backButton()).onPressed, isNull);
  });
}
