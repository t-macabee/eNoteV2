import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'config.dart';
import 'package:enote_core/enote_core.dart';
import 'features/admin/address/address_provider.dart';
import 'features/admin/music_store/music_store_provider.dart';
import 'shell/login_screen.dart';
import 'shell/master_screen.dart';

void main() {
  final authState = AuthState(baseUrl: kApiBaseUrl);
  final apiClient = ApiClient(baseUrl: kApiBaseUrl, authState: authState);

  runApp(MyApp(authState: authState, apiClient: apiClient));
}

class MyApp extends StatelessWidget {
  final AuthState authState;
  final ApiClient apiClient;

  const MyApp({
    super.key,
    required this.authState,
    required this.apiClient,
  });

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: authState),
        Provider<ApiClient>.value(value: apiClient),
        ProxyProvider<ApiClient, AddressProvider>(
          update: (_, apiClient, _) => AddressProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, MusicStoreProvider>(
          update: (_, apiClient, _) => MusicStoreProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        title: 'eNote V2',
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        ),
        home: Consumer<AuthState>(
          builder: (context, authState, _) {
            if (!authState.isAuthenticated) {
              return const LoginScreen();
            }
            return const MasterScreen();
          },
        ),
      ),
    );
  }
}
