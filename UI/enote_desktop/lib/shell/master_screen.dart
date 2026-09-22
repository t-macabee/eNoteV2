import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../features/profile/profile_dialog.dart' as enote_desktop_profile;
import '../features/store_employee/store/shop_store_form_screen.dart';
import '../features/store_employee/store/shop_store_provider.dart';
import '../widgets/dialog_shell.dart';
import '../widgets/entity_form_scaffold.dart';
import 'role_menu.dart';
import 'role_menu_entries.dart';

class MasterScreen extends StatefulWidget {
  const MasterScreen({super.key});

  @override
  State<MasterScreen> createState() => _MasterScreenState();
}

class _MasterScreenState extends State<MasterScreen> {
  RoleMenuEntry? _selectedEntry;
  AuthState? _authState;
  NotificationController? _notificationController;

  @override
  void initState() {
    super.initState();
    final roles = _currentRoles();
    for (final entry in kRoleMenuEntries) {
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
    context.read<AuthState>().logout(revoke: true);
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
            entries: kRoleMenuEntries,
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
