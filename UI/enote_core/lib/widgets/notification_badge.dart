import 'package:flutter/material.dart';

import '../notifications/notification_controller.dart';

/// Bell icon with an unread-count badge, driven by the same
/// [NotificationController] the full notification list uses — opening the
/// list and coming back never desyncs the badge from what's actually unread.
class NotificationBadge extends StatelessWidget {
  final NotificationController controller;
  final VoidCallback onTap;

  const NotificationBadge({
    super.key,
    required this.controller,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final count = controller.unreadCount;
        return Stack(
          clipBehavior: Clip.none,
          children: [
            IconButton(
              tooltip: 'Obavještenja',
              onPressed: onTap,
              icon: const Icon(Icons.notifications_outlined),
            ),
            if (count > 0)
              Positioned(
                right: 4,
                top: 4,
                child: IgnorePointer(
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                    constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                    decoration: BoxDecoration(
                      color: Colors.red.shade700,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      count > 99 ? '99+' : '$count',
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: Colors.white, fontSize: 10),
                    ),
                  ),
                ),
              ),
          ],
        );
      },
    );
  }
}
