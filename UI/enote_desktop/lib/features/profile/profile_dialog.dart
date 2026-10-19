import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';
import '../../theme/app_theme.dart';
import '../../widgets/entity_form_scaffold.dart';
import 'change_password_dialog.dart';
import 'edit_profile_dialog.dart';
import 'profile_provider.dart';

class ProfileDialog extends StatefulWidget {
  const ProfileDialog({super.key});

  @override
  State<ProfileDialog> createState() => _ProfileDialogState();
}

class _ProfileDialogState extends State<ProfileDialog> {
  UserProfileResponse? _profileResponse;
  bool _isLoading = true;
  int _pictureVersion = 0;

  @override
  void initState() {
    super.initState();
    _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    setState(() => _isLoading = true);
    try {
      final profileProvider = context.read<ProfileProvider>();
      final profile = await profileProvider.getProfile();
      if (mounted) {
        setState(() {
          _profileResponse = profile;
          _pictureVersion++;
        });
      }
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _openEditDialog() async {
    final profile = _profileResponse!;
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => EditProfileDialog(
        initialFirstName: profile.profile.firstName,
        initialLastName: profile.profile.lastName,
        initialEmail: profile.email,
        initialDateOfBirth: profile.profile.dateOfBirth,
        initialHasPicture: profile.hasPicture,
      ),
    );
    if (mounted) _fetchProfile();
  }

  Future<void> _openChangePasswordDialog() async {
    await EntityFormScaffold.showAsDialog(
      context,
      builder: (_) => const ChangePasswordDialog(),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Dialog(
        child: Padding(
          padding: EdgeInsets.all(40),
          child: CircularProgressIndicator(),
        ),
      );
    }

    if (_profileResponse == null) {
      return const Dialog(
        child: Padding(
          padding: EdgeInsets.all(40),
          child: Text('Greška pri učitavanju profila.'),
        ),
      );
    }

    final profile = _profileResponse!;
    final firstName = profile.profile.firstName ?? ' - ';
    final lastName = profile.profile.lastName ?? ' - ';
    final email = profile.email ?? ' - ';
    final dateOfBirthValue = profile.profile.dateOfBirth;
    final dateOfBirth =
        dateOfBirthValue != null ? formatDate(dateOfBirthValue) : ' - ';
    final username = profile.username.isNotEmpty
        ? profile.username
        : (context.read<AuthState>().username ?? '');
    final role = UserRole.fromString(profile.role);
    final roleLabel = role != null ? userRoleLabel(role) : profile.role;

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 440),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'Moj Profil',
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w600,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'Zatvori',
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Center(child: _buildAvatar(context, profile)),
              const SizedBox(height: 12),
              InfoRow(label: 'Korisničko ime', value: username, labelWidth: 120),
              InfoRow(label: 'Uloga', value: roleLabel, labelWidth: 120),
              InfoRow(label: 'Ime', value: firstName, labelWidth: 120),
              InfoRow(label: 'Prezime', value: lastName, labelWidth: 120),
              InfoRow(label: 'Email', value: email, labelWidth: 120),
              InfoRow(label: 'Datum rođenja', value: dateOfBirth, labelWidth: 120),
              const SizedBox(height: 24),
              Wrap(
                alignment: WrapAlignment.end,
                spacing: 12,
                runSpacing: 8,
                children: [
                  OutlinedButton.icon(
                    onPressed: _openEditDialog,
                    icon: const Icon(Icons.edit_outlined),
                    label: const Text('Uredi'),
                  ),
                  OutlinedButton.icon(
                    onPressed: _openChangePasswordDialog,
                    icon: const Icon(Icons.lock_outline),
                    label: const Text('Promijeni lozinku'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildAvatar(BuildContext context, UserProfileResponse profile) {
    Widget initialsAvatar() {
      return CircleAvatar(
        radius: 40,
        backgroundColor: AppTheme.primary,
        child: Text(
          _initials(
            profile.profile.firstName,
            profile.profile.lastName,
            profile.username,
          ),
          style: const TextStyle(
            fontSize: 28,
            fontWeight: FontWeight.w600,
            color: Colors.white,
          ),
        ),
      );
    }

    final provider = context.read<ProfileProvider>();
    final apiClient = context.read<ApiClient>();
    return networkImageOrPlaceholder(
      provider.pictureUrl(cacheBuster: _pictureVersion),
      apiClient,
      size: 80,
      borderRadius: 40,
      placeholder: initialsAvatar,
    );
  }

  static String _initials(
    String? firstName,
    String? lastName,
    String username,
  ) {
    final parts = [
      if (firstName != null && firstName.trim().isNotEmpty)
        firstName.trim()[0].toUpperCase(),
      if (lastName != null && lastName.trim().isNotEmpty)
        lastName.trim()[0].toUpperCase(),
    ];
    if (parts.isNotEmpty) return parts.take(2).join();
    final clean = username.trim();
    if (clean.isEmpty) return '?';
    return clean[0].toUpperCase();
  }
}
