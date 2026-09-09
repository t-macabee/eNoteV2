import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../theme/app_theme.dart';

/// Membership state card for S30. The three variants and their copy are
/// `02-design` §11 `membership.active / expired / none`.
class MembershipCard extends StatelessWidget {
  final DateTime? paidUntil;
  final bool isMembershipActive;

  const MembershipCard({
    super.key,
    required this.paidUntil,
    required this.isMembershipActive,
  });

  bool get _isNone => paidUntil == null;

  bool get _isActive =>
      paidUntil != null && isMembershipActive;

  @override
  Widget build(BuildContext context) {
    final Color tint;
    final Color foreground;
    final IconData icon;
    if (_isNone) {
      tint = AppTheme.warning;
      foreground = AppTheme.warning;
      icon = Icons.info_outline;
    } else if (_isActive) {
      tint = AppTheme.success;
      foreground = AppTheme.success;
      icon = Icons.check_circle_outline;
    } else {
      tint = AppTheme.error;
      foreground = AppTheme.error;
      icon = Icons.cancel_outlined;
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: tint.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: tint.withValues(alpha: 0.4)),
      ),
      child: Row(
        children: [
          Icon(icon, color: foreground, size: 24),
          const SizedBox(width: 12),
          Expanded(child: _copy(context)),
        ],
      ),
    );
  }

  Widget _copy(BuildContext context) {
    final until = paidUntil;
    if (_isNone) {
      return Text(
        'Članarina nije aktivna.',
        style: TextStyle(color: AppTheme.warning, fontWeight: FontWeight.w600),
      );
    }
    if (_isActive) {
      return Text(
        'Aktivna do ${formatDate(until!)}',
        style: TextStyle(color: AppTheme.success, fontWeight: FontWeight.w600),
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Članarina je istekla ${formatDate(until!)}',
          style: TextStyle(color: AppTheme.error, fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 2),
        Text(
          'Obratite se školi za obnovu.',
          style: const TextStyle(color: AppTheme.textSecondary, fontSize: 13),
        ),
      ],
    );
  }
}
