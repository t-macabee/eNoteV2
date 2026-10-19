import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_filter_dropdown.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../instructor/instructor_provider.dart';
import '../student/student_provider.dart';
import 'admin_user_provider.dart';
import 'store_employee_provider.dart';
import 'user_details_dialog.dart';
import 'user_list_item.dart';
import 'user_provision_form_screen.dart';

DateTime nextMonthClamped(DateTime from) {
  final lastDay = DateTime(from.year, from.month + 2, 0).day;
  return DateTime(from.year, from.month + 1, from.day.clamp(1, lastDay));
}

class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<UserListItem>>();

  UserRole? _role;

  bool _showActive = true;

  static const int _unionFetchCap = 100;

  bool _unionTruncated = false;

  void _setUnionTruncated(bool value) {
    if (_unionTruncated == value) return;
    _unionTruncated = value;
    if (mounted) setState(() {});
  }

  Future<void> _openProvisionForm() async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const UserProvisionFormScreen(
        presentation: EntityFormPresentation.dialog,
      ),
    );
    _gridKey.currentState?.refresh();
  }

  Future<DateTime?> _renewMembership(
    BuildContext context,
    UserListItem item,
  ) async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final paidUntil = item.membershipPaidUntil;
    final initialDate = nextMonthClamped(
      paidUntil != null && paidUntil.isAfter(today) ? paidUntil : now,
    );

    final pickedDate = await showDatePicker(
      context: context,
      initialDate: initialDate.isBefore(today) ? today : initialDate,
      firstDate: today,
      lastDate: DateTime(today.year + 10),
      helpText: 'Produži članstvo',
      confirmText: 'Sačuvaj',
      cancelText: 'Odustani',
    );

    if (pickedDate == null || !context.mounted) return null;

    try {
      final updated = await context
          .read<AdminUserProvider>()
          .renewMembership(item.appUserId, pickedDate);
      _gridKey.currentState?.refresh();
      return updated;
    } catch (e) {
      if (context.mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return null;
    }
  }

  Future<bool> _setUserStatus(
    BuildContext context,
    UserListItem item,
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
    UserListItem item,
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

  void _showUserDetailsDialog(BuildContext context, UserListItem item) {
    showDialog<void>(
      context: context,
      builder: (dialogContext) => UserDetailsDialog(
        item: item,
        onRenewMembership: () => _renewMembership(context, item),
        onStatusChange: (isActive) => _setUserStatus(context, item, isActive),
        onDelete: () => _deleteUser(context, item),
      ),
    );
  }

  void _applyFilters() {
    setState(() {});
    _gridKey.currentState?.refresh(resetPage: true);
  }

  Future<PagedResult<UserListItem>> _fetchUsers(
    int page,
    int pageSize,
    String search,
  ) async {
    final query = pagedQuery(
      page,
      pageSize,
      search,
      searchField: 'name',
      filters: {'isActive': _showActive},
    );

    if (_role == UserRole.instructor) {
      _setUnionTruncated(false);
      final result = await context.read<InstructorProvider>().search(query);
      return PagedResult<UserListItem>(
        items: result.items.map(UserListItem.fromInstructor).toList(),
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
      );
    }

    if (_role == UserRole.student) {
      _setUnionTruncated(false);
      final result = await context.read<StudentProvider>().search(query);
      return PagedResult<UserListItem>(
        items: result.items.map(UserListItem.fromStudent).toList(),
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
      );
    }

    if (_role == UserRole.storeEmployee) {
      _setUnionTruncated(false);
      final result = await context.read<StoreEmployeeProvider>().search(query);
      return PagedResult<UserListItem>(
        items: result.items.map(UserListItem.fromEmployee).toList(),
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
      );
    }

    final unionQuery = pagedQuery(
      1,
      _unionFetchCap,
      search,
      searchField: 'name',
      filters: {'isActive': _showActive},
    );
    final instructorFuture =
        context.read<InstructorProvider>().search(unionQuery);
    final studentFuture = context.read<StudentProvider>().search(unionQuery);
    final employeeFuture =
        context.read<StoreEmployeeProvider>().search(unionQuery);
    final results = await Future.wait([
      instructorFuture,
      studentFuture,
      employeeFuture,
    ]);
    final instructorResult = results[0] as PagedResult<InstructorDto>;
    final studentResult = results[1] as PagedResult<StudentDto>;
    final employeeResult = results[2] as PagedResult<ShopEmployeeDto>;

    _setUnionTruncated(
      (instructorResult.totalCount ?? 0) > _unionFetchCap ||
          (studentResult.totalCount ?? 0) > _unionFetchCap ||
          (employeeResult.totalCount ?? 0) > _unionFetchCap,
    );

    final merged = [
      ...instructorResult.items.map(UserListItem.fromInstructor),
      ...studentResult.items.map(UserListItem.fromStudent),
      ...employeeResult.items.map(UserListItem.fromEmployee),
    ];

    final start = (page - 1) * pageSize;
    final end = (start + pageSize).clamp(0, merged.length);
    final window =
        start >= merged.length ? <UserListItem>[] : merged.sublist(start, end);

    return PagedResult<UserListItem>(
      items: window,
      page: page,
      pageSize: pageSize,
      totalCount: merged.length,
    );
  }

  @override
  Widget build(BuildContext context) {
    return EntityGridScreen<UserListItem>(
      key: _gridKey,
      config: EntityGridConfig<UserListItem>(
        searchHint: 'Pretraži po imenu...',
        placeholderIcon: Icons.person_outline,
        trailing: _unionTruncated && _role == null
            ? const Center(
                child: Text(
                  'Prikazano prvih 100 — suzite pretragu po ulozi',
                  style: TextStyle(color: AppTheme.textSecondary),
                ),
              )
            : null,
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
          UserRole.instructor =>
            item.username != null ? '@${item.username}' : null,
          _ => item.username != null ? '@${item.username}' : null,
        },
        onTap: (context, item) => _showUserDetailsDialog(context, item),
        groupKeyOf: _role == null
            ? (item) => switch (item.role) {
                UserRole.instructor => 'Instruktori',
                UserRole.student => 'Studenti',
                UserRole.storeEmployee =>
                  userRoleLabel(UserRole.storeEmployee),
                _ => userRoleLabel(item.role),
              }
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
