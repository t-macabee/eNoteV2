import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

import '../../helpers.dart';
import 'fake_payment_sheet_gateway.dart';

const _baseUrl = 'http://10.0.2.2:5059/api/v1/';

/// Builds the app under test from the shared [AuthState]/[ApiClient]; each
/// payment screen supplies its own provider set and pushed screen.
typedef PaymentScreenAppBuilder = Widget Function(
  AuthState authState,
  ApiClient apiClient,
  String publishableKey,
);

/// Pumps one payment screen behind an `Open pay` button and waits for it to
/// settle. Shared by the tuition and rental payment screen tests.
Future<void> pumpPaymentScreen(
  WidgetTester tester, {
  required http.Client client,
  required FakePaymentSheetGateway gateway,
  String? publishableKey,
  required PaymentScreenAppBuilder builder,
}) async {
  tester.view.physicalSize = const Size(400, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);

  final authState = AuthState(
    baseUrl: _baseUrl,
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  final apiClient = ApiClient(
    baseUrl: _baseUrl,
    authState: authState,
    httpClient: client,
  );
  await tester.pumpWidget(
    builder(authState, apiClient, publishableKey ?? 'pk_test_123'),
  );
  await tester.tap(find.text('Open pay'));
  await tester.pumpAndSettle();
}
