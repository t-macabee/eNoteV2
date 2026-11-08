import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';

import '../../widgets/dialog_shell.dart';
import '../../widgets/entity_form_scaffold.dart';
import '../../widgets/inline_error_text.dart';
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
        setState(() => _profileResponse = profile);
      }
    } catch (_) {
      if (mounted) setState(() => _profileResponse = null);
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
    return DialogShell(
      title: 'Moj profil',
      width: DialogShellWidth.sm,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.lg,
        ),
        child: _buildContent(context),
      ),
    );
  }

  Widget _buildContent(BuildContext context) {
    if (_isLoading) {
      return const SizedBox(
        height: 120,
        child: Center(child: CircularProgressIndicator()),
      );
    }

    final profile = _profileResponse;
    if (profile == null) {
      return const InlineErrorText('Greška pri učitavanju profila.');
    }

    final dateOfBirth = profile.profile.dateOfBirth;
    final username = profile.username.isNotEmpty
        ? profile.username
        : (context.read<AuthState>().username ?? '');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Center(child: _buildAvatar(context, profile)),
        const SizedBox(height: AppSpacing.lg),
        InfoRow(
          icon: Icons.alternate_email,
          label: 'Korisničko ime',
          value: orDash(username),
        ),
        InfoRow(
          icon: Icons.badge_outlined,
          label: 'Uloga',
          value: roleLabelOf(profile.role),
        ),
        InfoRow(
          icon: Icons.person_outline,
          label: 'Ime',
          value: orDash(profile.profile.firstName),
        ),
        InfoRow(
          icon: Icons.person_outline,
          label: 'Prezime',
          value: orDash(profile.profile.lastName),
        ),
        InfoRow(
          icon: Icons.email_outlined,
          label: 'Email',
          value: orDash(profile.email),
        ),
        InfoRow(
          icon: Icons.cake_outlined,
          label: 'Datum rođenja',
          value: dateOfBirth != null ? formatDate(dateOfBirth) : orDash(null),
        ),
        const SizedBox(height: AppSpacing.xl),
        Wrap(
          alignment: WrapAlignment.end,
          spacing: AppSpacing.md,
          runSpacing: AppSpacing.sm,
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
    );
  }

  Widget _buildAvatar(BuildContext context, UserProfileResponse profile) {
    Widget initialsAvatar() {
      return InitialsAvatar(
        text: formatDisplayName(
          profile.profile.firstName,
          profile.profile.lastName,
          profile.username,
        ),
        radius: 40,
      );
    }

    final provider = context.read<ProfileProvider>();
    final apiClient = context.read<ApiClient>();
    return networkImageOrPlaceholder(
      provider.pictureUrl(),
      apiClient,
      size: 80,
      borderRadius: 40,
      placeholder: initialsAvatar,
    );
  }
}
