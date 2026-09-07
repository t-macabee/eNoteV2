import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/entity_grid_screen.dart';
import '../instructor/instructor_provider.dart';
import '../student/student_provider.dart';
import 'admin_user_provider.dart';
import 'store_employee_provider.dart';
import 'user_provision_form_screen.dart';

class _UserListItem {
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

  _UserListItem.fromInstructor(InstructorDto i)
      : appUserId = i.appUserId,
        displayName = _formatDisplayName(i.firstName, i.lastName, i.username),
        username = i.username,
        firstName = i.firstName,
        lastName = i.lastName,
        role = UserRole.instructor,
        storeName = null,
        membershipPaidUntil = null,
        enrollmentDate = null,
        isManager = null,
        isActive = i.isActive;

  _UserListItem.fromStudent(StudentDto s)
      : appUserId = s.appUserId,
        displayName = _formatDisplayName(s.firstName, s.lastName, s.username),
        username = s.username,
        firstName = s.firstName,
        lastName = s.lastName,
        role = UserRole.student,
        storeName = null,
        membershipPaidUntil = s.membershipPaidUntil,
        enrollmentDate = s.enrollmentDate,
        isManager = null,
        isActive = s.isActive;

  _UserListItem.fromEmployee(ShopEmployeeDto e)
      : appUserId = e.appUserId,
        displayName = _formatDisplayName(e.firstName, e.lastName, e.username),
        username = e.username,
        firstName = e.firstName,
        lastName = e.lastName,
        role = UserRole.storeEmployee,
        storeName = e.storeName,
        membershipPaidUntil = null,
        enrollmentDate = null,
        isManager = e.isManager,
        isActive = e.isActive;

  static String _formatDisplayName(
    String? firstName,
    String? lastName,
    String? username,
  ) {
    final name = '${firstName ?? ''} ${lastName ?? ''}'.trim();
    if (name.isNotEmpty) return name;
    return username ?? '-';
  }
}

/// Administrator "Users" tab — card grid of Students + Instructors + StoreEmployees,
/// filterable by name and role.
class UserGridScreen extends StatefulWidget {
  const UserGridScreen({super.key});

  @override
  State<UserGridScreen> createState() => _UserGridScreenState();
}

class _UserGridScreenState extends State<UserGridScreen> {
  final _gridKey = GlobalKey<EntityGridScreenState<_UserListItem>>();

  /// null = "Svi korisnici" (default) — shows Instruktori + Studenti + StoreEmployee as
  /// labeled sections. Otherwise filters to just that role.
  UserRole? _role;

  /// Admin-only filter for account standing. Unlike [_role] this has no
  /// "svi" option — it always applies, defaulting to active accounts so
  /// deactivated users don't clutter the default view.
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

  Future<DateTime?> _renewMembership(
    BuildContext context,
    _UserListItem item,
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
    _UserListItem item,
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
    _UserListItem item,
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

  void _showUserDetailsDialog(BuildContext context, _UserListItem item) {
    showDialog<void>(
      context: context,
      builder: (dialogContext) => _UserDetailsDialog(
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
  Future<PagedResult<_UserListItem>> _fetchUsers(
    int page,
    int pageSize,
    String search,
  ) async {
    final query = {
      'page': page,
      'pageSize': pageSize,
      'includeTotalCount': true,
      'isActive': _showActive,
      if (search.isNotEmpty) 'name': search,
    };

    if (_role == UserRole.instructor) {
      final result = await context.read<InstructorProvider>().search(query);
      return PagedResult<_UserListItem>(
        items: result.items.map(_UserListItem.fromInstructor).toList(),
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
      );
    }

    if (_role == UserRole.student) {
      final result = await context.read<StudentProvider>().search(query);
      return PagedResult<_UserListItem>(
        items: result.items.map(_UserListItem.fromStudent).toList(),
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
      );
    }

    if (_role == UserRole.storeEmployee) {
      final result = await context.read<StoreEmployeeProvider>().search(query);
      return PagedResult<_UserListItem>(
        items: result.items.map(_UserListItem.fromEmployee).toList(),
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
      );
    }

    // _role == null: fetch instructors, students, and store employees concurrently
    final instructorFuture = context.read<InstructorProvider>().search(query);
    final studentFuture = context.read<StudentProvider>().search(query);
    final employeeFuture = context.read<StoreEmployeeProvider>().search(query);
    final results = await Future.wait([
      instructorFuture,
      studentFuture,
      employeeFuture,
    ]);
    final instructorResult = results[0] as PagedResult<InstructorDto>;
    final studentResult = results[1] as PagedResult<StudentDto>;
    final employeeResult = results[2] as PagedResult<ShopEmployeeDto>;

    final totalCount = (instructorResult.totalCount != null ||
            studentResult.totalCount != null ||
            employeeResult.totalCount != null)
        ? (instructorResult.totalCount ?? 0) +
            (studentResult.totalCount ?? 0) +
            (employeeResult.totalCount ?? 0)
        : null;

    return PagedResult<_UserListItem>(
      items: [
        ...instructorResult.items.map(_UserListItem.fromInstructor),
        ...studentResult.items.map(_UserListItem.fromStudent),
        ...employeeResult.items.map(_UserListItem.fromEmployee),
      ],
      page: page,
      pageSize: pageSize,
      totalCount: totalCount,
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
    return EntityGridScreen<_UserListItem>(
      key: _gridKey,
      config: EntityGridConfig<_UserListItem>(
        searchHint: 'Pretraži po imenu...',
        placeholderIcon: Icons.person_outline,
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
            SizedBox(
              width: 220,
              child: DropdownButtonFormField<UserRole?>(
                isExpanded: true,
                initialValue: _role,
                decoration: const InputDecoration(labelText: 'Uloga'),
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

class _UserDetailsDialog extends StatefulWidget {
  final _UserListItem item;
  final Future<DateTime?> Function() onRenewMembership;
  final Future<bool> Function(bool) onStatusChange;
  final Future<bool> Function() onDelete;

  const _UserDetailsDialog({
    required this.item,
    required this.onRenewMembership,
    required this.onStatusChange,
    required this.onDelete,
  });

  @override
  State<_UserDetailsDialog> createState() => _UserDetailsDialogState();
}

class _UserDetailsDialogState extends State<_UserDetailsDialog> {
  DateTime? _membershipPaidUntil;
  UserProfile? _profile;
  String? _email;
  bool _isLoadingProfile = true;
  bool _isStatusChanging = false;
  late bool _isActive = widget.item.isActive ?? true;

  Future<void> _handleStatusChange() async {
    final nextStatus = !_isActive;
    final confirmed = await confirmDialog(
      context: context,
      title: nextStatus ? 'Potvrdite aktivaciju' : 'Potvrdite deaktivaciju',
      message: nextStatus ? 'Da li ste sigurni da želite da aktivirate ovog korisnika?' : 'Da li ste sigurni da želite da deaktivirate ovog korisnika?',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    setState(() => _isStatusChanging = true);
    try {
      final success = await widget.onStatusChange(nextStatus);
      if (success && mounted) {
        // Optimistically reflect the new status so the button label/action
        // and the delete-button-adjacent state update immediately — the
        // underlying grid may re-filter this user out of view entirely
        // (e.g. the "Aktivni" tab), but this dialog stays open and must
        // not keep offering the action that was just taken.
        setState(() => _isActive = nextStatus);
      }
    } finally {
      if (mounted) {
        setState(() => _isStatusChanging = false);
      }
    }
  }

  bool _isDeleting = false;

  Future<void> _handleDelete() async {
    final confirmed = await confirmDialog(
      context: context,
      title: 'Trajno brisanje',
      message: 'Da li ste sigurni da želite trajno obrisati ovog korisnika? Ovo se razlikuje od deaktivacije i ne može se poništiti.',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    setState(() => _isDeleting = true);
    try {
      final success = await widget.onDelete();
      if (success && mounted) {
        Navigator.of(context).pop();
      }
    } finally {
      if (mounted) {
        setState(() => _isDeleting = false);
      }
    }
  }

  Widget _buildDeactivateControl() {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        OutlinedButton.icon(
          onPressed:
              _isStatusChanging || _isLoadingProfile || _isDeleting ? null : _handleStatusChange,
          icon: _isStatusChanging
              ? SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: _isActive ? AppTheme.error : AppTheme.success,
                  ),
                )
              : Icon(
                  _isActive ? Icons.person_off_outlined : Icons.person_add_alt,
                  size: 18,
                ),
          label: Text(_isActive ? 'Deaktiviraj' : 'Aktiviraj'),
          style: OutlinedButton.styleFrom(
            foregroundColor: _isActive ? AppTheme.error : AppTheme.success,
            side: BorderSide(
              color: _isActive ? AppTheme.error : AppTheme.success,
            ),
          ),
        ),
        const SizedBox(width: 8),
        IconButton(
          onPressed: _isDeleting || _isLoadingProfile || _isStatusChanging ? null : _handleDelete,
          icon: _isDeleting
              ? const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.delete_outline),
          tooltip: 'Trajno obriši',
          style: IconButton.styleFrom(
            foregroundColor: AppTheme.error,
            hoverColor: AppTheme.error.withValues(alpha: 0.1),
          ),
        ),
      ],
    );
  }

  @override
  void initState() {
    super.initState();
    _membershipPaidUntil = widget.item.membershipPaidUntil;
    _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    try {
      final profileResponse = await context
          .read<AdminUserProvider>()
          .getProfile(widget.item.appUserId);
      if (mounted) {
        setState(() {
          _profile = profileResponse.profile;
          _email = profileResponse.email;
          if (_profile?.membershipPaidUntil != null) {
            _membershipPaidUntil = _profile!.membershipPaidUntil;
          }
        });
      }
    } catch (_) {
      // Ignored: fallback to basic item info
    } finally {
      if (mounted) {
        setState(() => _isLoadingProfile = false);
      }
    }
  }

  Future<void> _handleRenew() async {
    final updatedDate = await widget.onRenewMembership();
    if (updatedDate != null && mounted) {
      setState(() {
        _membershipPaidUntil = updatedDate;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final isMembershipActive = _membershipPaidUntil != null &&
        (_membershipPaidUntil!.isAfter(today) ||
            _membershipPaidUntil!.isAtSameMomentAs(today));

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 520),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundColor: AppTheme.primary,
                    child: Text(
                      _initials(item.displayName),
                      style: const TextStyle(
                        color: AppTheme.onPrimary,
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          item.displayName,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w600,
                            color: AppTheme.textPrimary,
                          ),
                        ),
                        if (item.username != null &&
                            item.username!.isNotEmpty) ...[
                          const SizedBox(height: 2),
                          Text(
                            '@${item.username}',
                            style: const TextStyle(
                              fontSize: 13,
                              color: AppTheme.textSecondary,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Chip(
                    label: Text(
                      item.role.label,
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    backgroundColor: AppTheme.primary.withValues(alpha: 0.12),
                    side: BorderSide.none,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(20),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 20),
              const Divider(height: 1),
              const SizedBox(height: 16),
              _buildInfoRow(
                icon: Icons.person_outline,
                label: 'Ime i prezime',
                value: item.displayName,
              ),
              if (_email != null && _email!.isNotEmpty)
                _buildInfoRow(
                  icon: Icons.email_outlined,
                  label: 'Email',
                  value: _email!,
                ),
              if (item.role == UserRole.storeEmployee) ...[
                if (item.storeName != null && item.storeName!.isNotEmpty)
                  _buildInfoRow(
                    icon: Icons.store_outlined,
                    label: 'Muzička prodavnica',
                    value: item.storeName!,
                  ),
                _buildInfoRow(
                  icon: Icons.badge_outlined,
                  label: 'Pozicija',
                  value: item.isManager == true
                      ? 'Voditelj radnje'
                      : 'Uposlenik radnje',
                ),
              ],
              if (item.role == UserRole.student) ...[
                if (item.enrollmentDate != null ||
                    _profile?.enrollmentDate != null)
                  _buildInfoRow(
                    icon: Icons.calendar_today_outlined,
                    label: 'Datum upisa',
                    value: formatDate(
                      item.enrollmentDate ?? _profile!.enrollmentDate!,
                    ),
                  ),
                _buildMembershipStatusRow(isMembershipActive),
              ],
              if (_isLoadingProfile) ...[
                const SizedBox(height: 12),
                const Center(
                  child: SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                ),
              ],
              const SizedBox(height: 24),
              Row(
                children: [
                  _buildDeactivateControl(),
                  const Spacer(),
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Zatvori'),
                  ),
                  if (item.role == UserRole.student) ...[
                    const SizedBox(width: 8),
                    FilledButton.icon(
                      onPressed: _handleRenew,
                      icon: const Icon(Icons.edit_calendar, size: 18),
                      label: const Text('Produži članstvo'),
                    ),
                  ],
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMembershipStatusRow(bool isActive) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            Icons.card_membership_outlined,
            size: 20,
            color: isActive ? Colors.green : Colors.orange,
          ),
          const SizedBox(width: 12),
          const SizedBox(
            width: 140,
            child: Text(
              'Status članarine:',
              style: TextStyle(
                color: AppTheme.textSecondary,
                fontSize: 13,
              ),
            ),
          ),
          Expanded(
            child: Row(
              children: [
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: isActive
                        ? Colors.green.withValues(alpha: 0.15)
                        : Colors.red.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    isActive ? 'Aktivna' : 'Istekla / Nema',
                    style: TextStyle(
                      color:
                          isActive ? Colors.green.shade700 : Colors.red.shade700,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  ),
                ),
                if (_membershipPaidUntil != null) ...[
                  const SizedBox(width: 8),
                  Text(
                    'do ${formatDate(_membershipPaidUntil!)}',
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppTheme.textPrimary,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow({
    required IconData icon,
    required String label,
    required String value,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: AppTheme.textSecondary),
          const SizedBox(width: 12),
          SizedBox(
            width: 140,
            child: Text(
              label,
              style: const TextStyle(
                color: AppTheme.textSecondary,
                fontSize: 13,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 13,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }

  static String _initials(String name) {
    final parts = name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty);
    if (parts.isEmpty) return '?';
    final chars = parts.take(2).map((p) => p[0].toUpperCase()).toList();
    return chars.join();
  }
}
