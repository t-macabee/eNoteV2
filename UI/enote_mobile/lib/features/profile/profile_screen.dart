import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/app_router.dart';
import '../../session/session_controller.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/labeled_value.dart';
import 'membership_card.dart';

/// S30 — profile view (tab 3 root). Reads [SessionController.profile] as the
/// single profile source; every mutation elsewhere goes through
/// `SessionController.reloadProfile()` so this screen always shows the
/// reloaded values on pop.
class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  Object? _error;

  Future<void> _reload() async {
    try {
      await context.read<SessionController>().reloadProfile();
      if (mounted) setState(() => _error = null);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    }
  }

  Future<void> _confirmLogout() async {
    final controller = context.read<SessionController>();
    final confirmed = await confirmDialog(
      context: context,
      title: 'Odjava',
      message: 'Da li ste sigurni da se želite odjaviti?',
    );
    if (confirmed == true) {
      await controller.logoutAndRevoke();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<SessionController>(
      builder: (context, controller, _) {
        final profile = controller.profile;
        if (profile == null) {
          return AsyncStateView(
            isLoading: _error == null,
            error: _error,
            onRetry: _reload,
            child: const SizedBox.shrink(),
          );
        }
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16),
            children: [
              const SizedBox(height: 8),
              Center(child: _avatar(controller, profile)),
              const SizedBox(height: 16),
              Text(
                formatDisplayName(
                  profile.profile.firstName,
                  profile.profile.lastName,
                  profile.username,
                ),
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 22,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                '@${profile.username}',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: AppTheme.textSecondary,
                  fontSize: 14,
                ),
              ),
              const SizedBox(height: 20),
              MembershipCard(
                paidUntil: controller.membershipPaidUntil,
                isMembershipActive: controller.isMembershipActive,
              ),
              const SizedBox(height: 24),
              _values(profile),
              const SizedBox(height: 28),
              FilledButton(
                onPressed: () => Navigator.of(
                  context,
                  rootNavigator: true,
                ).pushNamed(AppRouter.profileEdit),
                child: const Text('Uredi profil'),
              ),
              const SizedBox(height: 8),
              OutlinedButton(
                onPressed: () => Navigator.of(
                  context,
                  rootNavigator: true,
                ).pushNamed(AppRouter.profilePassword),
                child: const Text('Promijeni lozinku'),
              ),
              const SizedBox(height: 8),
              OutlinedButton(
                onPressed: _confirmLogout,
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppTheme.error,
                  side: const BorderSide(color: AppTheme.error),
                ),
                child: const Text('Odjavi se'),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _avatar(SessionController controller, UserProfileResponse profile) {
    Widget initialsAvatar() {
      return CircleAvatar(
        radius: 48,
        backgroundColor: AppTheme.primary,
        child: Text(
          _initials(profile.profile.firstName, profile.profile.lastName),
          style: const TextStyle(
            fontSize: 32,
            fontWeight: FontWeight.w600,
            color: Colors.white,
          ),
        ),
      );
    }

    if (!profile.hasPicture) return initialsAvatar();

    return networkImageOrPlaceholder(
      userPictureUrl(
        context.read<ApiClient>(),
        'me',
        cacheBuster: controller.pictureVersion,
      ),
      context.read<ApiClient>(),
      size: 96,
      borderRadius: 48,
      placeholder: initialsAvatar,
    );
  }

  Widget _values(UserProfileResponse profile) {
    final p = profile.profile;
    return Column(
      children: [
        LabeledValue(label: 'Email', value: orDash(profile.email)),
        LabeledValue(
          label: 'Datum rođenja',
          value: _dateOrDash(p.dateOfBirth),
        ),
        LabeledValue(
          label: 'Upisan od',
          value: _dateOrDash(p.enrollmentDate),
        ),
        LabeledValue(label: 'Adresa', value: _addressOrDash(p.address)),
      ],
    );
  }

  String _dateOrDash(DateTime? value) =>
      value == null ? '—' : formatDate(value);

  String _addressOrDash(UserAddressDto? address) {
    if (address == null) return '—';
    return '${address.street} ${address.number}, ${address.city}';
  }

  static String _initials(String? firstName, String? lastName) {
    final parts = [
      if (firstName != null && firstName.trim().isNotEmpty)
        firstName.trim()[0].toUpperCase(),
      if (lastName != null && lastName.trim().isNotEmpty)
        lastName.trim()[0].toUpperCase(),
    ];
    if (parts.isNotEmpty) return parts.take(2).join();
    return '?';
  }
}
