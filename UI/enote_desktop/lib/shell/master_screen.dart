import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../features/admin/address/address_list_screen.dart';
import '../features/admin/instrument_type/instrument_type_list_screen.dart';
import '../features/admin/music_store/music_store_list_screen.dart';
import 'role_menu.dart';

class MasterScreen extends StatefulWidget {
  const MasterScreen({super.key});

  @override
  State<MasterScreen> createState() => _MasterScreenState();
}

class _MasterScreenState extends State<MasterScreen> {
  static const _entries = <RoleMenuEntry>[
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
  ];

  RoleMenuEntry? _selectedEntry;

  static Widget _buildAddressList(BuildContext context) {
    return const AddressListScreen();
  }

  static Widget _buildMusicStoreList(BuildContext context) {
    return const MusicStoreListScreen();
  }

  static Widget _buildInstrumentTypeList(BuildContext context) {
    return const InstrumentTypeListScreen();
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

  @override
  Widget build(BuildContext context) {
    final roles = _currentRoles();

    return Scaffold(
      appBar: AppBar(
        title: const Text('eNote V2'),
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Odjava',
            onPressed: _logout,
          ),
        ],
      ),
      drawer: RoleMenu(
        entries: _entries,
        currentRoles: roles,
        selected: _selectedEntry,
        onSelect: _onEntrySelected,
        onLogout: _logout,
      ),
      body: _selectedEntry?.screenBuilder(context) ??
          const Center(
            child: Text('Molimo odaberite opciju iz izbornika.'),
          ),
    );
  }
}
