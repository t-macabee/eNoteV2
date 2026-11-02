import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/routes.dart';
import '../../session/session_controller.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/detail_actions.dart';
import '../../widgets/labeled_value.dart';
import '../student_notices.dart';
import 'delete_profile_dialog.dart';

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

  Future<void> _deleteProfile() async {
    final messenger = ScaffoldMessenger.of(context);
    final controller = context.read<SessionController>();
    final deleted = await showDeleteProfileDialog(context);
    if (deleted != true || !mounted) return;
    await controller.endSession();
    messenger.showSnackBar(const SnackBar(content: Text('Profil je obrisan.')));
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<SessionController>(
      builder: (context, controller, _) {
        final profile = controller.profile;
        if (profile == null) {
          final error = _error ?? controller.bootstrapError;
          return AsyncStateView(
            isLoading: error == null,
            error: error,
            onRetry: _reload,
            child: const SizedBox.shrink(),
          );
        }
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.only(bottom: AppSpacing.lg),
            children: [
              const SizedBox(height: AppSpacing.lg),
              Center(child: _avatar(controller, profile)),
              const SizedBox(height: AppSpacing.lg),
              Text(
                formatDisplayName(
                  profile.profile.firstName,
                  profile.profile.lastName,
                  profile.username,
                ),
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: AppSpacing.xxs),
              Text(
                '@${profile.username}',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium
                    ?.copyWith(color: AppTheme.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                child: _membershipBanner(controller),
              ),
              const SizedBox(height: AppSpacing.xl),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                child: _values(profile),
              ),
              DetailActions(
                actions: [
                  FilledButton(
                    onPressed: () => Navigator.of(
                      context,
                      rootNavigator: true,
                    ).pushNamed(AppRoutes.profileEdit),
                    child: const Text('Uredi profil'),
                  ),
                  OutlinedButton(
                    onPressed: () => Navigator.of(
                      context,
                      rootNavigator: true,
                    ).pushNamed(AppRoutes.profilePassword),
                    child: const Text('Promijeni lozinku'),
                  ),
                  OutlinedButton(
                    onPressed: _confirmLogout,
                    child: const Text('Odjavi se'),
                  ),
                  OutlinedButton(
                    onPressed: _deleteProfile,
                    style: dangerOutlinedButtonStyle,
                    child: const Text('Obriši profil'),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _avatar(SessionController controller, UserProfileResponse profile) {
    final apiClient = context.read<ApiClient>();
    return networkImageOrPlaceholder(
      userPictureUrl(apiClient, 'me', cacheBuster: controller.pictureVersion),
      apiClient,
      size: AppSize.avatarRadiusLg * 2,
      borderRadius: AppSize.avatarRadiusLg,
      placeholder: () => InitialsAvatar(
        text: formatDisplayName(
          profile.profile.firstName,
          profile.profile.lastName,
          profile.username,
        ),
        radius: AppSize.avatarRadiusLg,
      ),
    );
  }

  Widget _membershipBanner(SessionController controller) {
    final paidUntil = controller.membershipPaidUntil;
    if (paidUntil == null) {
      return const StatusBanner(
        icon: Icons.info_outline,
        message: 'Članarina nije aktivna.',
        tone: StatusTone.warning,
      );
    }
    if (controller.isMembershipActive) {
      return StatusBanner(
        icon: Icons.check_circle_outline,
        message: 'Aktivna do ${formatDate(paidUntil)}',
        tone: StatusTone.success,
      );
    }
    return StatusBanner(
      icon: Icons.cancel_outlined,
      message: membershipNotice(paidUntil),
      tone: StatusTone.danger,
    );
  }

  Widget _values(UserProfileResponse profile) {
    final p = profile.profile;
    return Column(
      children: [
        LabeledValue(label: 'Email', value: orDash(profile.email)),
        LabeledValue(label: 'Datum rođenja', value: _dateOrDash(p.dateOfBirth)),
        LabeledValue(label: 'Upisan od', value: _dateOrDash(p.enrollmentDate)),
      ],
    );
  }

  String _dateOrDash(DateTime? value) =>
      value == null ? '—' : formatDate(value);
}
