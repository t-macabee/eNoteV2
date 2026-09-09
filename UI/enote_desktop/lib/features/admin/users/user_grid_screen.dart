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
import 'user_provision_form_screen.dart';

class UserListItem {
  final int appUserId;
  final String displayName;
  final String? username;
  final String? firstName;
  final String? lastName;
  final UserRole role;
  final String? storeName;
  final DateTime? membershipPaidUntil;
  final DateTime? enrollmentDate;
  final bool? isManager;
  final bool? isActive;

  UserListItem.fromInstructor(InstructorDto i)
      : appUserId = i.appUserId,
        displayName = formatDisplayName(i.firstName, i.lastName, i.username),
        username = i.username,
        firstName = i.firstName,
        lastName = i.lastName,
        role = UserRole.instructor,
        storeName = null,
        membershipPaidUntil = null,
        enrollmentDate = null,
        isManager = null,
        isActive = i.isActive;

  UserListItem.fromStudent(StudentDto s)
      : appUserId = s.appUserId,
        displayName = formatDisplayName(s.firstName, s.lastName, s.username),
        username = s.username,
        firstName = s.firstName,
        lastName = s.lastName,
        role = UserRole.student,
        storeName = null,
        membershipPaidUntil = s.membershipPaidUntil,
        enrollmentDate = s.enrollmentDate,
        isManager = null,
        isActive = s.isActive;

  UserListItem.fromEmployee(ShopEmployeeDto e)
      : appUserId = e.appUserId,
        displayName = formatDisplayName(e.firstName, e.lastName, e.username),
        username = e.username,
        firstName = e.firstName,
        lastName = e.lastName,
        role = UserRole.storeEmployee,
        storeName = e.storeName,
        membershipPaidUntil = null,
        enrollmentDate = null,
        isManager = e.isManager,
        isActive = e.isActive;
}

/// Administrator "Users" tab — card grid of Students + Instructors + StoreEmployees,
/// filterable by name and role.
class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<UserListItem>>();

  /// null = "Svi korisnici" (default) — shows Instruktori + Studenti + StoreEmployee as
  /// labeled sections. Otherwise filters to just that role.
  UserRole? _role;

  /// Admin-only filter for account standing. Unlike [_role] this has no
  /// "svi" option — it always applies, defaulting to active accounts so
  /// deactivated users don't clutter the default view.
  bool _showActive = true;

  /// Fixed per-role fetch cap for the all-roles ("Svi korisnici") merge view.
  /// Each role endpoint is asked for page 1 up to this many rows, the three
  /// lists are merged client-side, and the requested page window is sliced
  /// from the merged list. Keeps the union view correct without backend work.
  static const int _unionFetchCap = 100;

  /// True when any role endpoint reported more rows than [_unionFetchCap],
  /// meaning the merge view is truncated. Surfaced as a one-line hint above
  /// the grid (via `trailing`) so truncation is never silent.
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
    final initialDate = (item.membershipPaidUntil != null &&
            item.membershipPaidUntil!.isAfter(today))
        ? DateTime(
            item.membershipPaidUntil!.year,
            item.membershipPaidUntil!.month + 1,
            item.membershipPaidUntil!.day,
          )
        : DateTime(now.year, now.month + 1, now.day);

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

  /// Loads one page for the current [_role] / [_showActive] filters.
  ///
  /// Reads `_role` and `_showActive` as fields at call time rather than
  /// taking them as parameters: `setState` doesn't rebuild synchronously, so
  /// `_applyFilters` calling `_gridKey.currentState?.refresh()` right after
  /// `setState` can still run against the previous build's config. Reading
  /// the fields here keeps the fetch correct regardless of when the config
  /// that points at this method was created.
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

    // _role == null: the three role endpoints have independent server-side
    // paging, so requesting the same page/pageSize from each and
    // concatenating repeats/skips rows. Instead fetch page 1 up to a fixed
    // cap from each role, merge client-side, then slice the requested window.
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
    // `_fetchUsers` reads `_role` and `_showActive` as fields at call time
    // rather than taking them as parameters snapshotted here — `setState`
    // doesn't rebuild synchronously, so `_applyFilters` calling
    // `_gridKey.currentState?.refresh()` right after `setState` can still
    // be running against this build's (about-to-be-stale) config. See the
    // doc comment on `_fetchUsers`.
    return EntityGridScreen<UserListItem>(
      key: _gridKey,
      config: EntityGridConfig<UserListItem>(
        searchHint: 'Pretraži po imenu...',
        placeholderIcon: Icons.person_outline,
        // Visible only when the all-roles merge view hit the per-role cap.
        trailing: _unionTruncated && _role == null
            ? const Center(
                child: Text(
                  'Prikazano prvih 100 — suzite pretragu po ulozi',
                  style: TextStyle(color: AppTheme.textSecondary),
                ),
              )
            : null,
        // Avatar image only — a 404 (no picture / not visible) falls back to
        // the placeholder icon. Hover-card content below is untouched.
        imageUrlOf: (item) =>
            userPictureUrl(context.read<ApiClient>(), item.appUserId),
        titleOf: (item) => item.displayName,
        // Membership date is deliberately left off the hover card — tapping
        // the card opens `_UserDetailsDialog`, which already shows the full
        // membership status and a "Produži članstvo" button, so repeating
        // the date here is redundant.
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
        // No cardActions here: "Produži članstvo" already lives in
        // `_UserDetailsDialog`, which `onTap` opens — a second copy on the
        // hover overlay was pure duplication.
        onTap: (context, item) => _showUserDetailsDialog(context, item),
        groupKeyOf: _role == null
            ? (item) => switch (item.role) {
                UserRole.instructor => 'Instruktori',
                UserRole.student => 'Studenti',
                UserRole.storeEmployee => 'StoreEmployee',
                _ => item.role.label,
              }
            : null,
        filterBar: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            EntityFilterDropdown<UserRole?>(
              label: 'Uloga',
              width: 220,
              value: _role,
              items: const [
                DropdownMenuItem(value: null, child: Text('Svi korisnici')),
                DropdownMenuItem(
                  value: UserRole.instructor,
                  child: Text('Instruktor'),
                ),
                DropdownMenuItem(
                    value: UserRole.student, child: Text('Student')),
                DropdownMenuItem(
                  value: UserRole.storeEmployee,
                  child: Text('StoreEmployee'),
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
