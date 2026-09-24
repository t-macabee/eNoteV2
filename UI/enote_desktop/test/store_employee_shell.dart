import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/address/address_provider.dart';
import 'package:enote_desktop/features/shared/announcement/store_announcement_provider.dart';
import 'package:enote_desktop/features/store_employee/employee/shop_employee_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/instrument_provider.dart';
import 'package:enote_desktop/features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'package:enote_desktop/features/store_employee/rental/rental_provider.dart';
import 'package:enote_desktop/features/store_employee/store/shop_store_provider.dart';
import 'package:enote_desktop/shell/master_screen.dart';

import 'helpers.dart';

ScriptedClient storeEmployeeShellClient() => ScriptedClient((request) {
  final url = request.url.toString();
  if (request.method == 'DELETE') {
    return jsonResponse('', 204);
  }

  if (request.method == 'GET') {
    if (url.contains('shop/store')) {
      return jsonResponse(const {
        'id': 10,
        'storeName': 'Muzička Prodavnica',
        'businessHours': '08:00 - 16:00',
        'phoneNumber': '+387 61 111 222',
        'addressId': 2,
        'addressStreet': 'Ferhadija 15',
        'addressCity': 'Sarajevo',
        'imagePath': '/images/store.jpg',
      }, 200);
    }
    if (url.contains('shop/instruments')) {
      return jsonResponse(const {
        'items': [
          {
            'id': 1,
            'model': 'Stratocaster',
            'manufacturer': 'Fender',
            'instrumentTypeId': 1,
            'instrumentType': 'Gitara',
            'musicStore': 'Muzička Prodavnica',
            'isAvailable': true,
          }
        ],
        'page': 1,
        'pageSize': 20,
        'totalCount': 1,
      }, 200);
    }
    if (url.contains('shop/instrument-types')) {
      return jsonResponse(const {
        'items': [
          {'id': 1, 'type': 'Gitara', 'monthlyFee': 50.0},
        ],
        'page': 1,
        'pageSize': 100,
        'totalCount': 1,
      }, 200);
    }
    if (url.contains('admin/addresses')) {
      return jsonResponse(const {
        'items': [
          {
            'id': 1,
            'cityId': 1,
            'city': 'Sarajevo',
            'street': 'Maršala Tita',
            'number': '1'
          },
          {
            'id': 2,
            'cityId': 1,
            'city': 'Sarajevo',
            'street': 'Ferhadija',
            'number': '15'
          },
        ],
        'page': 1,
        'pageSize': 100,
        'totalCount': 2,
      }, 200);
    }
    if (url.contains('notifications')) {
      return jsonResponse(const {'items': [], 'unreadCount': 0}, 200);
    }
  }

  if (request.method == 'PUT' && url.contains('shop/store')) {
    return jsonResponse(const {
      'id': 10,
      'storeName': 'Ažurirana Prodavnica',
      'businessHours': '09:00 - 17:00',
      'phoneNumber': '+387 61 999 888',
      'addressId': 2,
      'addressStreet': 'Ferhadija 15',
      'addressCity': 'Sarajevo',
    }, 200);
  }

  if (request.method == 'GET') {
    return jsonResponse(const {
      'items': [],
      'page': 1,
      'pageSize': 20,
      'totalCount': 0,
    }, 200);
  }

  return jsonResponse(const {}, 200);
});

Widget buildStoreEmployeeShell({
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
  final notificationController = NotificationController(
    apiClient: apiClient,
    endpoint: 'notifications',
  );
  final employeeProvider = ShopEmployeeProvider(apiClient: apiClient);
  final rentalProvider = RentalProvider(apiClient: apiClient);
  final announcementProvider = StoreAnnouncementProvider(apiClient: apiClient);

  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthState>.value(value: authState),
      Provider<ApiClient>.value(value: apiClient),
      ChangeNotifierProvider<NotificationController>.value(value: notificationController),
      ChangeNotifierProvider<ShopStoreProvider>.value(value: storeProvider),
      ChangeNotifierProvider<AddressProvider>.value(value: addressProvider),
      ChangeNotifierProvider<InstrumentProvider>.value(value: instrumentProvider),
      ChangeNotifierProvider<ShopInstrumentTypeProvider>.value(value: instrumentTypeProvider),
      ChangeNotifierProvider<ShopEmployeeProvider>.value(value: employeeProvider),
      ChangeNotifierProvider<RentalProvider>.value(value: rentalProvider),
      ChangeNotifierProvider<StoreAnnouncementProvider>.value(value: announcementProvider),
    ],
    child: const MaterialApp(
      home: MasterScreen(),
    ),
  );
}
