import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../theme/app_theme.dart';

const _kSidebarWidth = 280.0;

class RoleMenuEntry {
  final IconData icon;
  final String label;
  final WidgetBuilder screenBuilder;
  final List<UserRole> allowedRoles;
  final bool isDialog;
  final bool dividerBefore;

  const RoleMenuEntry({
    required this.icon,
    required this.label,
    required this.screenBuilder,
    required this.allowedRoles,
    this.isDialog = false,
    this.dividerBefore = false,
  });
}

class RoleMenu extends StatelessWidget {
  final List<RoleMenuEntry> entries;
  final List<UserRole> currentRoles;
  final RoleMenuEntry? selected;
  final ValueChanged<RoleMenuEntry> onSelect;
  final VoidCallback? onLogout;
  final VoidCallback? onNotificationsTap;

  const RoleMenu({
    super.key,
    required this.entries,
    required this.currentRoles,
    required this.onSelect,
    this.selected,
    this.onLogout,
    this.onNotificationsTap,
  });

  List<RoleMenuEntry> get visibleEntries => entries
      .where((entry) => entry.allowedRoles.any(currentRoles.contains))
      .toList();

  @override
  Widget build(BuildContext context) {
    final authState = context.watch<AuthState>();
    final username = authState.username;
    final topRole = authState.topRole;
    final notificationController = context.watch<NotificationController>();

    return Container(
      width: _kSidebarWidth,
      color: AppTheme.background,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 24, 16, 20),
            child: Row(
              children: [
                Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    color: AppTheme.primary,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: const Icon(
                    Icons.bar_chart,
                    size: 20,
                    color: AppTheme.onPrimary,
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: const [
                      Text(
                        'eNote',
                        style: TextStyle(
                          color: AppTheme.textPrimary,
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                          fontStyle: FontStyle.italic,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              children: [
                for (final entry in visibleEntries) ...[
                  if (entry.dividerBefore)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 8, horizontal: 8),
                      child: Divider(color: AppTheme.outline, height: 1),
                    ),
                  _NavRow(
                    icon: entry.icon,
                    label: entry.label,
                    selected: !entry.isDialog && identical(entry, selected),
                    onTap: () => onSelect(entry),
                  ),
                ],
              ],
            ),
          ),
          if (onLogout != null || username != null)
            Container(
              decoration: const BoxDecoration(
                border: Border(
                  top: BorderSide(
                    color: AppTheme.outline,
                    width: 1,
                  ),
                ),
              ),
              padding: const EdgeInsets.fromLTRB(16, 12, 12, 12),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 16,
                    backgroundColor: AppTheme.primary,
                    child: Text(
                      _initials(username),
                      style: const TextStyle(
                        color: AppTheme.onPrimary,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          username ?? '',
                          style: const TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 13,
                            fontWeight: FontWeight.w500,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        if (topRole != null)
                          Text(
                            topRole,
                            style: const TextStyle(
                              color: AppTheme.textSecondary,
                              fontSize: 11,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                      ],
                    ),
                  ),
                  if (onNotificationsTap != null)
                    NotificationBadge(
                      controller: notificationController,
                      onTap: onNotificationsTap!,
                    ),
                  if (onLogout != null)
                    IconButton(
                      icon: const Icon(Icons.logout, size: 18),
                      tooltip: 'Odjava',
                      onPressed: onLogout,
                    ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  static String _initials(String? username) {
    if (username == null || username.isEmpty) return '?';
    final name = username.split('@').first;
    final parts = name.split(RegExp(r'[._\s-]+')).where((p) => p.isNotEmpty);
    if (parts.isEmpty) return '?';
    final chars = parts
        .take(2)
        .map((p) => p.substring(0, 1).toUpperCase())
        .toList();
    return chars.join();
  }
}

class _NavRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _NavRow({
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final color = selected ? AppTheme.primary : AppTheme.textSecondary;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Material(
        color: selected
            ? AppTheme.primary.withValues(alpha: 0.12)
            : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
        child: InkWell(
          borderRadius: BorderRadius.circular(8),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            child: Row(
              children: [
                Icon(icon, size: 20, color: color),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    label,
                    style: TextStyle(
                      color: color,
                      fontSize: 13.5,
                      fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
