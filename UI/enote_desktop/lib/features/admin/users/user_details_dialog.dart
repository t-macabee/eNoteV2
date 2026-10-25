import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/inline_error_text.dart';
import '../../shared/membership/membership_section.dart';
import '../student/student_provider.dart';
import 'admin_user_provider.dart';

class UserDetailsDialog extends StatefulWidget {
  final AdminUserListItemDto item;
  final Future<bool> Function(bool) onStatusChange;
  final Future<bool> Function() onDelete;

  const UserDetailsDialog({
    super.key,
    required this.item,
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
  bool _profileError = false;
  bool _isStatusChanging = false;
  late bool _isActive = widget.item.isActive;

  Future<void> _handleStatusChange() async {
    final nextStatus = !_isActive;
    final confirmed = await confirmDialog(
      context: context,
      title: nextStatus ? 'Potvrdite aktivaciju' : 'Potvrdite deaktivaciju',
      message: nextStatus ? 'Da li ste sigurni da želite da aktivirate korisnika "${widget.item.displayName}"?' : 'Da li ste sigurni da želite da deaktivirate korisnika "${widget.item.displayName}"?',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    setState(() => _isStatusChanging = true);
    try {
      final success = await widget.onStatusChange(nextStatus);
      if (success && mounted) {
        setState(() => _isActive = nextStatus);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              nextStatus ? 'Korisnik je aktiviran.' : 'Korisnik je deaktiviran.',
            ),
          ),
        );
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
      message: 'Da li ste sigurni da želite trajno obrisati korisnika "${widget.item.displayName}"? Ovo se razlikuje od deaktivacije i ne može se poništiti.',
    );
    if (confirmed != true) return;
    if (!mounted) return;

    setState(() => _isDeleting = true);
    try {
      final success = await widget.onDelete();
      if (success && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Korisnik je obrisan.')),
        );
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
          _profileError = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _profileError = true);
      }
    } finally {
      if (mounted) {
        setState(() => _isLoadingProfile = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 520,
          maxHeight: MediaQuery.sizeOf(context).height * 0.85,
        ),
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
                      item.role == null
                          ? 'Nepoznata uloga'
                          : userRoleLabel(item.role!),
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
              Flexible(
                child: SingleChildScrollView(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
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
                        if (item.storeName != null &&
                            item.storeName!.isNotEmpty)
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
                              item.enrollmentDate ??
                                  _profile!.enrollmentDate!,
                            ),
                          ),
                        const SizedBox(height: 8),
                        _buildMembershipSection(item),
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
                      ] else if (_profileError) ...[
                        const SizedBox(height: 12),
                        const InlineErrorText('Greška pri učitavanju profila.'),
                      ],
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),
              Row(
                children: [
                  _buildDeactivateControl(),
                  const Spacer(),
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Zatvori'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMembershipSection(AdminUserListItemDto item) {
    final studentId = item.studentId;
    if (studentId == null) return const SizedBox.shrink();

    return MembershipSection(
      api: context.read<StudentProvider>(),
      studentId: studentId,
      membershipPaidUntil: _membershipPaidUntil,
      onMembershipChanged: (paidUntil) =>
          setState(() => _membershipPaidUntil = paidUntil),
    );
  }

  static String _initialsFromName(String name) {
    final parts = name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty);
    if (parts.isEmpty) return '?';
    final chars = parts.take(2).map((p) => p[0].toUpperCase()).toList();
    return chars.join();
  }
}
