import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'config.dart';
import 'package:enote_core/enote_core.dart';
import 'theme/app_theme.dart';
import 'features/admin/address/address_provider.dart';
import 'features/admin/city/city_provider.dart';
import 'features/admin/course/admin_course_provider.dart';
import 'features/admin/event/event_provider.dart';
import 'features/admin/instructor/instructor_provider.dart';
import 'features/admin/instrument_type/instrument_type_provider.dart';
import 'features/instructor/course/course_provider.dart';
import 'features/instructor/lecture/lecture_provider.dart';
import 'features/admin/music_store/music_store_provider.dart';
import 'features/admin/users/user_provision_service.dart';
import 'features/store_employee/announcement/announcement_provider.dart';
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
        ChangeNotifierProvider<AddressProvider>(
          create: (_) => AddressProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<CityProvider>(
          create: (_) => CityProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<MusicStoreProvider>(
          create: (_) => MusicStoreProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<InstrumentTypeProvider>(
          create: (_) => InstrumentTypeProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<InstructorProvider>(
          create: (_) => InstructorProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<EventProvider>(
          create: (_) => EventProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<CourseProvider>(
          create: (_) => CourseProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<AdminCourseProvider>(
          create: (_) => AdminCourseProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<LectureProvider>(
          create: (_) => LectureProvider(apiClient: apiClient),
        ),
        // Note: LectureNoteProvider is NOT registered globally — its endpoint
        // depends on the lecture being viewed (instructor/lectures/{lectureId}/notes),
        // so it is instantiated per-navigation via a screen-scoped
        // ChangeNotifierProvider in LectureListScreen._openNotes.
        ChangeNotifierProvider<InstrumentProvider>(
          create: (_) => InstrumentProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<ShopInstrumentTypeProvider>(
          create: (_) => ShopInstrumentTypeProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<RentalProvider>(
          create: (_) => RentalProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<StoreAnnouncementProvider>(
          create: (_) => StoreAnnouncementProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider<NotificationController>(
          create: (_) => NotificationController(
            apiClient: apiClient,
            endpoint: 'student/notifications',
          ),
        ),
        Provider<UserProvisionService>(
          create: (_) => UserProvisionService(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        title: 'eNote V2',
        theme: AppTheme.dark,
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
