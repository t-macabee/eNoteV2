import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../features/admin/course/admin_course_list_screen.dart';
import '../features/admin/event/event_list_screen.dart';
import '../features/admin/music_store/music_store_list_screen.dart';
import '../features/admin/users/user_grid_screen.dart';
import '../features/instructor/course/course_list_screen.dart';
import '../features/instructor/student/instructor_student_list_screen.dart';
import '../features/store_employee/announcement/announcement_list_screen.dart';
import '../features/store_employee/employee/shop_employee_list_screen.dart';
import '../features/store_employee/instrument/instrument_list_screen.dart';
import '../features/store_employee/rental/rental_list_screen.dart';
import 'role_menu.dart';

class MasterScreen extends StatefulWidget {
  const MasterScreen({super.key});

  @override
  State<MasterScreen> createState() => _MasterScreenState();
}

class _MasterScreenState extends State<MasterScreen> {
  // Administrator shell shape (Admin IA rework): 4 top-level tabs — Users,
  // Music Stores, Courses, Events. Gradovi/Adrese/Tipovi instrumenata no
  // longer have standalone sidebar entries (their screens/providers still
  // exist on disk, just unrouted from here — see the rework prompt, point 6,
  // for the still-open question of where their CRUD should live). Instructor
  // accounts are folded into the Users tab; the separate "Instruktori" entry
  // is gone.
  static const _entries = <RoleMenuEntry>[
    RoleMenuEntry(
      icon: Icons.people_outline,
      label: 'Korisnici',
      screenBuilder: _buildUserGrid,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.store,
      label: 'Muzičke prodavnice',
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
      icon: Icons.class_,
      label: 'Kursevi',
      screenBuilder: _buildCourseList,
      allowedRoles: [UserRole.instructor],
    ),
    RoleMenuEntry(
      icon: Icons.school_outlined,
      label: 'Studenti',
      screenBuilder: _buildInstructorStudentList,
      allowedRoles: [UserRole.instructor],
    ),
    RoleMenuEntry(
      icon: Icons.piano,
      label: 'Instrumenti',
      screenBuilder: _buildInstrumentList,
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

  static Widget _buildInstrumentList(BuildContext context) {
    return const InstrumentListScreen();
  }

  static Widget _buildRentalList(BuildContext context) {
    return const RentalListScreen();
  }

  static Widget _buildAnnouncementList(BuildContext context) {
    return const AnnouncementListScreen();
  }

  @override
  void initState() {
    super.initState();
    final roles = _currentRoles();
    for (final entry in _entries) {
      if (entry.allowedRoles.any(roles.contains)) {
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
    setState(() {
      _selectedEntry = entry;
    });
  }

  void _openNotifications() {
    final controller = context.read<NotificationController>();
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => NotificationListView(controller: controller),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final roles = _currentRoles();

    return Scaffold(
      body: Row(
        children: [
          RoleMenu(
            entries: _entries,
            currentRoles: roles,
            selected: _selectedEntry,
            onSelect: _onEntrySelected,
            onLogout: _logout,
            onNotificationsTap: _openNotifications,
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
