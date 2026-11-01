import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/store_employee/dashboard/employee_dashboard_screen.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_provider.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';
import 'package:enote_desktop/shell/shell_navigation.dart';
import 'package:enote_desktop/widgets/dashboard_stat_card.dart';

import '../../../helpers.dart';

void _useDesktopView(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

http.Response _page(int totalCount) => jsonResponse({
  'items': [],
  'page': 1,
  'pageSize': 1,
  'totalCount': totalCount,
}, 200);

ScriptedClient _client({bool failRentals = false, bool failEmployees = false}) {
  return ScriptedClient((request) {
    final url = request.url.toString();
    if (request.method == 'GET' && url.contains('shop/store')) {
      return jsonResponse(const {
        'id': 1,
        'storeName': 'Muzička Prodavnica',
        'businessHours': '08:00 - 16:00',
      }, 200);
    }
    if (request.method == 'GET' && url.contains('shop/employees')) {
      if (failEmployees) {
        return jsonResponse({'message': 'Greška'}, 500);
      }
      return _page(6);
    }
    if (request.method == 'GET' && url.contains('shop/rentals')) {
      if (failRentals) {
        return jsonResponse({'message': 'Greška'}, 500);
      }
      return _page(2);
    }
    return _page(0);
  });
}

Widget _buildApp({
  required ScriptedClient httpClient,
  required void Function(String label) openEntry,
}) {
  final authState = AuthState(
    baseUrl: 'http://localhost:5059/api/v1/',
    tokenReader: () =>
        fakeJwt(username: 'employee', role: 'StoreEmployee', isManager: true),
  );
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: authState,
    httpClient: httpClient,
  );

  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<RentalProvider>(
        create: (_) => RentalProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<ShopEmployeeProvider>(
        create: (_) => ShopEmployeeProvider(apiClient: apiClient),
      ),
      ChangeNotifierProvider<ShopStoreProvider>(
        create: (_) => ShopStoreProvider(apiClient: apiClient),
      ),
    ],
    child: MaterialApp(
      home: ShellNavigation(
        openEntry: openEntry,
        child: const Scaffold(body: EmployeeDashboardScreen()),
      ),
    ),
  );
}

void main() {
  testWidgets('shows pending rental requests and employees counts', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    expect(find.text('2'), findsOneWidget);
    expect(find.text('6'), findsOneWidget);
    expect(find.textContaining('Muzička Prodavnica'), findsOneWidget);
    expect(find.text('–'), findsNothing);
  });

  testWidgets('one failed request shows a dash and keeps the other cards', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    await tester.pumpWidget(
      _buildApp(httpClient: _client(failEmployees: true), openEntry: (_) {}),
    );
    await tester.pumpAndSettle();

    expect(find.text('–'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
  });

  testWidgets('employees card is not tappable', (WidgetTester tester) async {
    _useDesktopView(tester);
    String? opened;
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (label) => opened = label),
    );
    await tester.pumpAndSettle();

    final card = tester.widget<DashboardStatCard>(
      find.byWidgetPredicate(
        (widget) =>
            widget is DashboardStatCard && widget.label == 'Zaposlenici',
      ),
    );
    expect(card.onTap, isNull);

    await tester.tap(find.text('Zaposlenici'));
    await tester.pump();

    expect(opened, isNull);
  });

  testWidgets('tapping the pending requests card opens the Zahtjevi entry', (
    WidgetTester tester,
  ) async {
    _useDesktopView(tester);
    String? opened;
    await tester.pumpWidget(
      _buildApp(httpClient: _client(), openEntry: (label) => opened = label),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Zahtjevi na čekanju'));
    await tester.pump();

    expect(opened, 'Zahtjevi');
  });
}
