import 'package:flutter/material.dart';

import '../notifications/notification_controller.dart';

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
        return IconButton(
          tooltip: 'Obavještenja',
          onPressed: onTap,
          icon: Badge(
            isLabelVisible: count > 0,
            label: Text(count > 99 ? '99+' : '$count'),
            child: const Icon(Icons.notifications_outlined),
          ),
        );
      },
    );
  }
}
