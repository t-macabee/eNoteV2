import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'config.dart';
import 'package:enote_core/enote_core.dart';
import 'features/admin/address/address_provider.dart';
import 'features/admin/instructor/instructor_provider.dart';
import 'features/admin/instrument_type/instrument_type_provider.dart';
import 'features/instructor/course/course_provider.dart';
import 'features/instructor/lecture/lecture_provider.dart';
import 'features/admin/music_store/music_store_provider.dart';
import 'features/admin/users/user_provision_service.dart';
import 'features/store_employee/instrument/instrument_provider.dart';
import 'features/store_employee/instrument/shop_instrument_type_provider.dart';
import 'features/store_employee/rental/rental_provider.dart';
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
        ProxyProvider<ApiClient, InstrumentTypeProvider>(
          update: (_, apiClient, _) => InstrumentTypeProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, InstructorProvider>(
          update: (_, apiClient, _) => InstructorProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, CourseProvider>(
          update: (_, apiClient, _) => CourseProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, LectureProvider>(
          update: (_, apiClient, _) => LectureProvider(apiClient: apiClient),
        ),
        // Note: LectureNoteProvider is NOT registered globally — its endpoint
        // depends on the lecture being viewed (instructor/lectures/{lectureId}/notes),
        // so it is instantiated per-navigation via a screen-scoped
        // ChangeNotifierProvider in LectureListScreen._openNotes.
        ProxyProvider<ApiClient, InstrumentProvider>(
          update: (_, apiClient, _) => InstrumentProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, ShopInstrumentTypeProvider>(
          update: (_, apiClient, _) =>
              ShopInstrumentTypeProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, RentalProvider>(
          update: (_, apiClient, _) => RentalProvider(apiClient: apiClient),
        ),
        ProxyProvider<ApiClient, UserProvisionService>(
          update: (_, apiClient, _) => UserProvisionService(apiClient: apiClient),
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
