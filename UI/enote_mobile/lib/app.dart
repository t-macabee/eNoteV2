import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../session/token_store.dart';
import '../theme/app_theme.dart';
import 'shell/app_router.dart';
import 'shell/session_gate.dart';

final GlobalKey<NavigatorState> appNavigatorKey = GlobalKey<NavigatorState>();

class EnoteMobileApp extends StatelessWidget {
  final AuthState authState;
  final ApiClient apiClient;
  final TokenStore tokenStore;

  const EnoteMobileApp({
    super.key,
    required this.authState,
    required this.apiClient,
    required this.tokenStore,
  });

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        Provider<TokenStore>.value(value: tokenStore),
      ],
      child: MaterialApp(
        navigatorKey: appNavigatorKey,
        theme: AppTheme.dark,
        onGenerateRoute: AppRouter.onGenerateRoute,
        home: const SessionGate(),
      ),
    );
  }
}
