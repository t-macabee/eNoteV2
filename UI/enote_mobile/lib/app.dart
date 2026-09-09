import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../session/deep_link_handler.dart';
import '../session/session_controller.dart';
import '../session/token_store.dart';
import '../theme/app_theme.dart';
import 'config.dart';
import 'features/auth/auth_provider.dart';
import 'features/profile/profile_provider.dart';
import 'realtime/notification_hub_client.dart';
import 'shell/app_router.dart';
import 'shell/session_gate.dart';

final GlobalKey<NavigatorState> appNavigatorKey = GlobalKey<NavigatorState>();

class EnoteMobileApp extends StatefulWidget {
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
  State<EnoteMobileApp> createState() => _EnoteMobileAppState();
}

class _EnoteMobileAppState extends State<EnoteMobileApp> {
  final DeepLinkHandler _deepLinkHandler = DeepLinkHandler();
  StreamSubscription<Uri>? _linkSubscription;

  @override
  void initState() {
    super.initState();
    _linkSubscription = AppLinks().uriLinkStream.listen((uri) {
      _deepLinkHandler.onLink(uri, appNavigatorKey);
    });
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _deepLinkHandler.flush(appNavigatorKey);
    });
  }

  @override
  void dispose() {
    _linkSubscription?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: widget.authState),
        Provider<ApiClient>.value(value: widget.apiClient),
        Provider<TokenStore>.value(value: widget.tokenStore),
        ChangeNotifierProvider<NotificationController>(
          create: (_) => NotificationController(
            apiClient: widget.apiClient,
            endpoint: 'student/notifications',
          ),
          lazy: false,
        ),
        Provider<NotificationHubClient>(
          create: (_) => NotificationHubClient(
            hubUrl: NotificationHubClient.deriveHubUrl(kApiBaseUrl),
            tokenProvider: () async => widget.authState.accessToken ?? '',
          ),
          dispose: (_, client) => client.dispose(),
        ),
        ChangeNotifierProvider<SessionController>(
          create: (context) => SessionController(
            apiClient: widget.apiClient,
            authState: widget.authState,
            notifications: context.read<NotificationController>(),
            onStopRealtime: () {
              context.read<NotificationHubClient>().stop();
            },
          ),
          lazy: false,
        ),
        Provider<AuthProvider>(
          create: (_) => AuthProvider(apiClient: widget.apiClient),
        ),
        Provider<ProfileProvider>(
          create: (_) => ProfileProvider(apiClient: widget.apiClient),
        ),
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
