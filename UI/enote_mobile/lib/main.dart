import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';

import 'package:enote_core/enote_core.dart';

import 'app.dart';
import 'config.dart';
import 'session/session_controller.dart';
import 'session/token_store.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final tokenStore = TokenStore();
  await tokenStore.load();
  late AuthState authState;
  late ApiClient apiClient;
  final sessionHttp = SessionHttpClient(
    onUnauthorized: () {
      SessionController.markSessionExpired();
      appNavigatorKey.currentState?.popUntil((route) => route.isFirst);
      authState.logout();
      SessionController.stopRealtime?.call();
    },
  );
  authState = AuthState(
    baseUrl: kApiBaseUrl,
    tokenReader: tokenStore.read,
    tokenWriter: tokenStore.write,
    tokenRevoker: () => apiClient.post('auth/logout'),
    httpClient: sessionHttp,
  );
  apiClient = ApiClient(
    baseUrl: kApiBaseUrl,
    authState: authState,
    httpClient: sessionHttp,
  );
  if (kStripePublishableKey.isNotEmpty) {
    Stripe.publishableKey = kStripePublishableKey;
  }
  runApp(
    EnoteMobileApp(
      authState: authState,
      apiClient: apiClient,
      tokenStore: tokenStore,
    ),
  );
}
