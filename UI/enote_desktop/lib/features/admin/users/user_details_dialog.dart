import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import 'admin_user_provider.dart';
import 'user_grid_screen.dart';

class UserDetailsDialog extends StatefulWidget {
  final UserListItem item;
  final Future<DateTime?> Function() onRenewMembership;
  final Future<bool> Function(bool) onStatusChange;
  final Future<bool> Function() onDelete;

  const UserDetailsDialog({
    super.key,
    required this.item,
    required this.onRenewMembership,
    required this.onStatusChange,
    required this.onDelete,
  });

  @override
  State<UserDetailsDialog> createState() => _UserDetailsDialogState();
}

class _UserDetailsDialogState extends State<UserDetailsDialog> {
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
                  UserAvatar(
                    userId: item.appUserId,
                    apiClient: context.read<ApiClient>(),
                    radius: 28,
                    // Avatar image only — a 404 falls back to the existing
                    // initials. Nothing else in this dialog changes.
                    placeholder: () => CircleAvatar(
                      radius: 28,
                      backgroundColor: AppTheme.primary,
                      child: Text(
                        _initialsFromName(item.displayName),
                        style: const TextStyle(
                          color: AppTheme.onPrimary,
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
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
              InfoRow(
                icon: Icons.person_outline,
                label: 'Ime i prezime',
                value: item.displayName,
              ),
              if (_email != null && _email!.isNotEmpty)
                InfoRow(
                  icon: Icons.email_outlined,
                  label: 'Email',
                  value: _email!,
                ),
              if (item.role == UserRole.storeEmployee) ...[
                if (item.storeName != null && item.storeName!.isNotEmpty)
                  InfoRow(
                    icon: Icons.store_outlined,
                    label: 'Muzička prodavnica',
                    value: item.storeName!,
                  ),
                InfoRow(
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
                  InfoRow(
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

  static String _initialsFromName(String name) {
    final parts = name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty);
    if (parts.isEmpty) return '?';
    final chars = parts.take(2).map((p) => p[0].toUpperCase()).toList();
    return chars.join();
  }
}
