import 'package:flutter/material.dart';
import 'package:flutter_stripe/flutter_stripe.dart';

import 'package:enote_core/enote_core.dart';

import 'app.dart';
import 'config.dart';
import 'session/session_http_client.dart';
import 'session/token_store.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final tokenStore = TokenStore();
  await tokenStore.load();
  late AuthState authState;
  final sessionHttp = SessionHttpClient(
    onUnauthorized: () => authState.logout(),
  );
  authState = AuthState(
    baseUrl: kApiBaseUrl,
    tokenReader: tokenStore.read,
    tokenWriter: tokenStore.write,
    httpClient: sessionHttp,
  );
  final apiClient = ApiClient(
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
