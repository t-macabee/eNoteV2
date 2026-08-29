import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../features/admin/address/address_list_screen.dart';
import '../features/admin/city/city_list_screen.dart';
import '../features/admin/instructor/instructor_list_screen.dart';
import '../features/admin/instrument_type/instrument_type_list_screen.dart';
import '../features/admin/music_store/music_store_list_screen.dart';
import '../features/admin/users/user_provision_form_screen.dart';
import '../features/instructor/course/course_list_screen.dart';
import '../features/store_employee/announcement/announcement_list_screen.dart';
import '../features/store_employee/instrument/instrument_list_screen.dart';
import '../features/store_employee/rental/rental_list_screen.dart';
import 'role_menu.dart';

class MasterScreen extends StatefulWidget {
  const MasterScreen({super.key});

  @override
  State<MasterScreen> createState() => _MasterScreenState();
}

class _MasterScreenState extends State<MasterScreen> {
  static const _entries = <RoleMenuEntry>[
    RoleMenuEntry(
      icon: Icons.location_city,
      label: 'Gradovi',
      screenBuilder: _buildCityList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.location_on,
      label: 'Adrese',
      screenBuilder: _buildAddressList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.store,
      label: 'Muzičke prodavnice',
      screenBuilder: _buildMusicStoreList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.music_note,
      label: 'Tipovi instrumenata',
      screenBuilder: _buildInstrumentTypeList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.school,
      label: 'Instruktori',
      screenBuilder: _buildInstructorList,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.class_,
      label: 'Kursevi',
      screenBuilder: _buildCourseList,
      allowedRoles: [UserRole.instructor],
    ),
    RoleMenuEntry(
      icon: Icons.person_add,
      label: 'Korisnici',
      screenBuilder: _buildUserProvision,
      allowedRoles: [UserRole.administrator],
    ),
    RoleMenuEntry(
      icon: Icons.piano,
      label: 'Instrumenti',
      screenBuilder: _buildInstrumentList,
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

  static Widget _buildCityList(BuildContext context) {
    return const CityListScreen();
  }

  static Widget _buildAddressList(BuildContext context) {
    return const AddressListScreen();
  }

  static Widget _buildMusicStoreList(BuildContext context) {
    return const MusicStoreListScreen();
  }

  static Widget _buildInstrumentTypeList(BuildContext context) {
    return const InstrumentTypeListScreen();
  }

  static Widget _buildInstructorList(BuildContext context) {
    return const InstructorListScreen();
  }

  static Widget _buildCourseList(BuildContext context) {
    return const CourseListScreen();
  }

  static Widget _buildUserProvision(BuildContext context) {
    return const UserProvisionFormScreen();
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
          ),
          Expanded(
            child: Scaffold(
              appBar: AppBar(
                title: const Text('eNote V2'),
                actions: [
                  Consumer<NotificationController>(
                    builder: (context, controller, _) => NotificationBadge(
                      controller: controller,
                      onTap: _openNotifications,
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.logout),
                    tooltip: 'Odjava',
                    onPressed: _logout,
                  ),
                ],
              ),
              body: _selectedEntry?.screenBuilder(context) ??
                  const Center(
                    child: Text('Molimo odaberite opciju iz izbornika.'),
                  ),
            ),
          ),
        ],
      ),
    );
  }
}
