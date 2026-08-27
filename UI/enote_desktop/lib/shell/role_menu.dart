import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

class RoleMenuEntry {
  final IconData icon;
  final String label;
  final WidgetBuilder screenBuilder;
  final List<UserRole> allowedRoles;

  const RoleMenuEntry({
    required this.icon,
    required this.label,
    required this.screenBuilder,
    required this.allowedRoles,
  });
}

class RoleMenu extends StatelessWidget {
  final List<RoleMenuEntry> entries;
  final List<UserRole> currentRoles;
  final RoleMenuEntry? selected;
  final ValueChanged<RoleMenuEntry> onSelect;
  final VoidCallback? onLogout;

  const RoleMenu({
    super.key,
    required this.entries,
    required this.currentRoles,
    required this.onSelect,
    this.selected,
    this.onLogout,
  });

  List<RoleMenuEntry> get visibleEntries => entries
      .where((entry) => entry.allowedRoles.any(currentRoles.contains))
      .toList();

  @override
  Widget build(BuildContext context) {
    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          for (final entry in visibleEntries)
            ListTile(
              leading: Icon(entry.icon),
              title: Text(entry.label),
              selected: identical(entry, selected),
              onTap: () => onSelect(entry),
            ),
          if (onLogout != null) ...[
            const Divider(),
            ListTile(
              leading: const Icon(Icons.logout),
              title: const Text('Odjava'),
              onTap: onLogout,
            ),
          ],
        ],
      ),
    );
  }
}
