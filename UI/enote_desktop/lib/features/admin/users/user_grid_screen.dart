import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import 'admin_user_provider.dart';
import 'user_details_dialog.dart';
import 'user_provision_form_screen.dart';

class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey =
      GlobalKey<EntityGridScreenState<AdminUserListItemDto>>();

  UserRole? _role;

  bool _showActive = true;

  Future<void> _openProvisionForm() async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const UserProvisionFormScreen(
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _gridKey.currentState?.refresh();
  }

  Future<bool> _setUserStatus(
    BuildContext context,
    AdminUserListItemDto item,
    bool isActive,
  ) async {
    try {
      await context
          .read<AdminUserProvider>()
          .setStatus(item.appUserId, isActive);
      _gridKey.currentState?.refresh();
      return true;
    } catch (e) {
      if (context.mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return false;
    }
  }

  Future<bool> _deleteUser(
    BuildContext context,
    AdminUserListItemDto item,
  ) async {
    try {
      await context.read<AdminUserProvider>().remove(item.appUserId);
      _gridKey.currentState?.refresh();
      return true;
    } catch (e) {
      if (context.mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return false;
    }
  }

  Future<void> _showUserDetailsDialog(
    BuildContext context,
    AdminUserListItemDto item,
  ) async {
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => UserDetailsDialog(
        item: item,
        onStatusChange: (isActive) => _setUserStatus(context, item, isActive),
        onDelete: () => _deleteUser(context, item),
      ),
    );
    _gridKey.currentState?.refresh();
  }

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  Future<PagedResult<AdminUserListItemDto>> _fetchUsers(
    int page,
    int pageSize,
    String search,
  ) {
    final query = pagedQuery(
      page,
      pageSize,
      search,
      searchField: 'name',
      filters: {
        'isActive': _showActive,
        if (_role != null) 'role': _role!.label,
      },
    );

    return context.read<AdminUserProvider>().getUsers(query);
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<AdminUserListItemDto>(
      key: _gridKey,
      config: EntityGridConfig<AdminUserListItemDto>(
        searchHint: 'Pretraži po imenu...',
        placeholderIcon: Icons.person_outline,
        imageUrlOf: (item) =>
            userPictureUrl(context.read<ApiClient>(), item.appUserId),
        titleOf: (item) => item.displayName,
        subtitleOf: (item) => switch (item.role) {
          UserRole.student => item.membershipPaidUntil == null
              ? (item.username != null ? '@${item.username}' : 'Student')
              : null,
          UserRole.storeEmployee =>
            item.storeName != null && item.storeName!.isNotEmpty
                ? item.storeName
                : (item.username != null ? '@${item.username}' : null),
          _ => item.username != null ? '@${item.username}' : null,
        },
        onTap: (context, item) => _showUserDetailsDialog(context, item),
        groupKeyOf: _role == null
            ? (item) =>
                item.role == null ? 'Ostalo' : userRoleLabel(item.role!)
            : null,
        filterBar: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            EntityFilterDropdown<UserRole?>(
              label: 'Uloga',
              width: 220,
              value: _role,
              items: [
                const DropdownMenuItem(
                  value: null,
                  child: Text('Svi korisnici'),
                ),
                DropdownMenuItem(
                  value: UserRole.administrator,
                  child: Text(userRoleLabel(UserRole.administrator)),
                ),
                DropdownMenuItem(
                  value: UserRole.instructor,
                  child: Text(userRoleLabel(UserRole.instructor)),
                ),
                DropdownMenuItem(
                  value: UserRole.student,
                  child: Text(userRoleLabel(UserRole.student)),
                ),
                DropdownMenuItem(
                  value: UserRole.storeEmployee,
                  child: Text(userRoleLabel(UserRole.storeEmployee)),
                ),
              ],
              onChanged: (role) {
                _role = role;
                _applyFilters();
              },
            ),
            const SizedBox(width: 12),
            SizedBox(
              height: 48,
              child: SegmentedButton<bool>(
                segments: const [
                  ButtonSegment(value: true, label: Text('Aktivni')),
                  ButtonSegment(value: false, label: Text('Neaktivni')),
                ],
                selected: {_showActive},
                onSelectionChanged: (newSelection) {
                  _showActive = newSelection.first;
                  _applyFilters();
                },
              ),
            ),
          ],
        ),
        fetcher: _fetchUsers,
        onAdd: () => _openProvisionForm(),
      ),
    );
  }
}
