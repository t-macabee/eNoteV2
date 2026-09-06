import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';
import '../../theme/app_theme.dart';
import '../../widgets/entity_form_scaffold.dart';
import 'change_password_dialog.dart';
import 'edit_profile_dialog.dart';

/// Read-only summary of the logged-in user's account, with "Uredi" and
/// "Promijeni lozinku" opening their own dedicated forms (each with its own
/// Save/Cancel), the same way editing a music store opens its own form
/// rather than editing inline.
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
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.get('users/me');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        if (mounted) {
          setState(() {
            _profileResponse = UserProfileResponse.fromJson(data);
          });
        }
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
      ),
    );
    // The edit dialog stays open on save and only clears its own fields, so
    // this summary is refreshed once it's finally closed.
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
              _buildInfoRow('Korisničko ime', username),
              _buildInfoRow('Uloga', profile.role),
              _buildInfoRow('Ime', firstName),
              _buildInfoRow('Prezime', lastName),
              _buildInfoRow('Email', email),
              _buildInfoRow('Datum rođenja', dateOfBirth),
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

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
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
}
