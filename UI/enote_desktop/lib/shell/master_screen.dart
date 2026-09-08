import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../features/profile/profile_dialog.dart' as enote_desktop_profile;
import '../features/admin/course/admin_course_list_screen.dart';
import '../features/admin/event/event_list_screen.dart';
import '../features/admin/music_store/music_store_list_screen.dart';
import '../features/admin/reference_data/reference_data_dialog.dart';
import '../features/admin/users/user_grid_screen.dart';
import '../features/instructor/course/course_list_screen.dart';
import '../features/instructor/course/course_catalog_screen.dart';
import '../features/instructor/student/instructor_student_list_screen.dart';
import '../features/shared/announcement/announcement_list_screen.dart';
import '../features/shared/announcement/store_announcement_provider.dart';
import '../features/store_employee/employee/shop_employee_list_screen.dart';
import '../features/store_employee/rental/rental_list_screen.dart';
import '../features/store_employee/store/shop_store_form_screen.dart';
import '../features/store_employee/store/shop_store_provider.dart';
import '../features/store_employee/store/shop_store_screen.dart';
import '../widgets/dialog_shell.dart';
import '../widgets/entity_form_scaffold.dart';
import '../widgets/entity_list_screen.dart';
import 'role_menu.dart';

class MasterScreen extends StatefulWidget {
  const MasterScreen({super.key});

  @override
  State<MasterScreen> createState() => _MasterScreenState();
}

class _MasterScreenState extends State<MasterScreen> {
  // Administrator shell shape: primary tabs (Korisnici, Trgovina muzičke
  // opreme, Kursevi, Događaji) followed by reference data CRUD (Gradovi, Adrese,
  // Tipovi instrumenata). Instructor accounts are folded into the Users tab.
  static const _entries = <RoleMenuEntry>[
    RoleMenuEntry(
      icon: Icons.people_outline,
      label: 'Korisnici',
      screenBuilder: _buildUserGrid,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.store,
      label: 'Trgovina muzičke opreme',
      screenBuilder: _buildMusicStoreList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.class_,
      label: 'Kursevi',
      screenBuilder: _buildAdminCourseList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.event,
      label: 'Događaji',
      screenBuilder: _buildEventList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.dataset_outlined,
      label: 'Referentni podaci',
      screenBuilder: _buildReferenceDataDialog,
      allowedRoles: [UserRole.administrator],
      isDialog: true,
      dividerBefore: true,
    ),
    RoleMenuEntry(
      icon: Icons.class_,
      label: 'Kursevi',
      screenBuilder: _buildCourseList,
      allowedRoles: [UserRole.instructor],
    ),
    RoleMenuEntry(
      icon: Icons.menu_book_outlined,
      label: 'Katalog kurseva',
      screenBuilder: _buildCourseCatalog,
      allowedRoles: [UserRole.instructor],
    ),
    RoleMenuEntry(
      icon: Icons.school_outlined,
      label: 'Studenti',
      screenBuilder: _buildInstructorStudentList,
      allowedRoles: [UserRole.instructor],
    ),
    RoleMenuEntry(
      icon: Icons.storefront_outlined,
      label: 'Moja prodavnica',
      screenBuilder: _buildShopStoreScreen,
      allowedRoles: [UserRole.storeEmployee],
    ),
    RoleMenuEntry(
      icon: Icons.badge_outlined,
      label: 'Zaposlenici',
      screenBuilder: _buildShopEmployeeList,
      allowedRoles: [UserRole.storeEmployee],
    ),
    RoleMenuEntry(
      icon: Icons.assignment,
      label: 'Zahtjevi',
      screenBuilder: _buildRentalList,
      allowedRoles: [UserRole.storeEmployee],
    ),
    RoleMenuEntry(
      icon: Icons.campaign,
      label: 'Objave',
      screenBuilder: _buildAnnouncementList,
      allowedRoles: [UserRole.storeEmployee],
    ),
  ];

  RoleMenuEntry? _selectedEntry;
  AuthState? _authState;
  NotificationController? _notificationController;

  static Widget _buildMusicStoreList(BuildContext context) {
    return const MusicStoreListScreen();
  }

  static Widget _buildCourseList(BuildContext context) {
    return const CourseListScreen();
  }

  static Widget _buildCourseCatalog(BuildContext context) {
    return const CourseCatalogScreen();
  }

  static Widget _buildInstructorStudentList(BuildContext context) {
    return const InstructorStudentListScreen();
  }

  static Widget _buildShopEmployeeList(BuildContext context) {
    return const ShopEmployeeListScreen();
  }

  static Widget _buildUserGrid(BuildContext context) {
    return const UserGridScreen();
  }

  static Widget _buildAdminCourseList(BuildContext context) {
    return const AdminCourseListScreen();
  }

  static Widget _buildEventList(BuildContext context) {
    return const EventListScreen();
  }

  static Widget _buildReferenceDataDialog(BuildContext context) {
    return const ReferenceDataDialog();
  }

  static Widget _buildRentalList(BuildContext context) {
    return const RentalListScreen(
      presentation: EntityListPresentation.embedded,
    );
  }

  static Widget _buildAnnouncementList(BuildContext context) {
    return AnnouncementListScreen(
      provider: context.read<StoreAnnouncementProvider>(),
      presentation: EntityListPresentation.embedded,
    );
  }

  static Widget _buildShopStoreScreen(BuildContext context) {
    return const ShopStoreScreen();
  }

  @override
  void initState() {
    super.initState();
    final roles = _currentRoles();
    for (final entry in _entries) {
      if (!entry.isDialog && entry.allowedRoles.any(roles.contains)) {
        _selectedEntry = entry;
        break;
      }
    }
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final authState = context.read<AuthState>();
    final notificationController = context.read<NotificationController>();
    if (!identical(_authState, authState)) {
      _authState?.removeListener(_onAuthChanged);
      _authState = authState;
      _authState!.addListener(_onAuthChanged);
    }
    if (!identical(_notificationController, notificationController)) {
      _notificationController = notificationController;
      if (authState.isAuthenticated) {
        _notificationController!.startPolling();
      }
    }
  }

  void _onAuthChanged() {
    final authState = _authState;
    final controller = _notificationController;
    if (authState == null || controller == null) return;
    if (authState.isAuthenticated) {
      controller.startPolling();
    } else {
      controller.stopPolling();
    }
  }

  @override
  void dispose() {
    _authState?.removeListener(_onAuthChanged);
    _notificationController?.stopPolling();
    super.dispose();
  }

  List<UserRole> _currentRoles() {
    return context
        .read<AuthState>()
        .roles
        .map(UserRole.fromString)
        .whereType<UserRole>()
        .toList();
  }

  void _logout() {
    context.read<AuthState>().logout();
  }

  void _onEntrySelected(RoleMenuEntry entry) {
    if (entry.isDialog) {
      showDialog<void>(
        context: context,
        builder: (dialogContext) => entry.screenBuilder(dialogContext),
      );
      return;
    }
    setState(() {
      _selectedEntry = entry;
    });
  }

  void _openNotifications() {
    final controller = context.read<NotificationController>();
    showDialog<void>(
      context: context,
      builder: (dialogContext) => DialogShell(
        width: DialogShellWidth.sm,
        header: DialogShellHeader(
          label: const Text(
            'Obavještenja',
            style: TextStyle(
              fontWeight: FontWeight.w600,
              fontSize: 16,
            ),
          ),
          action: TextButton(
            onPressed: controller.markAllRead,
            child: const Text('Označi sve kao pročitano'),
          ),
          onClose: () => Navigator.of(dialogContext).pop(),
        ),
        body: ConstrainedBox(
          constraints: BoxConstraints(
            maxHeight: MediaQuery.sizeOf(context).height * 0.6,
          ),
          child: NotificationListView(controller: controller),
        ),
      ),
    );
  }

  void _openProfile() {
    showDialog<void>(
      context: context,
      builder: (context) => const enote_desktop_profile.ProfileDialog(),
    );
  }

  Future<void> _openEditStore() async {
    final storeProvider = context.read<ShopStoreProvider>();
    MusicStoreDto? store = storeProvider.store;
    if (store == null) {
      try {
        store = await storeProvider.getOwnStore();
      } catch (e) {
        if (mounted) {
          ErrorBanner.show(context, message: userMessage(e));
        }
        return;
      }
    }
    if (!mounted) return;
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => ShopStoreFormScreen(
        store: store!,
        presentation: EntityFormPresentation.dialog,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final roles = _currentRoles();
    final isStoreEmployee = roles.contains(UserRole.storeEmployee);
    final isManager = context.watch<AuthState>().isManager;

    return Scaffold(
      body: Row(
        children: [
          RoleMenu(
            entries: _entries,
            currentRoles: roles,
            selected: _selectedEntry,
            onSelect: _onEntrySelected,
            onOpenProfile: _openProfile,
            onLogout: _logout,
            onNotificationsTap: _openNotifications,
            onEditStore: (isStoreEmployee && isManager) ? _openEditStore : null,
          ),
          Expanded(
            child: _selectedEntry?.screenBuilder(context) ??
                const Center(
                  child: Text('Molimo odaberite opciju iz izbornika.'),
                ),
          ),
        ],
      ),
    );
  }
}
