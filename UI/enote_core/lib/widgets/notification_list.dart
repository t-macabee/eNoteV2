import 'package:flutter/material.dart';

import '../models/communication/communication_models.dart';
import '../notifications/notification_controller.dart';
import 'error_banner.dart';
import '../formatting/formatters.dart';
/// Full notification screen, driven by the same [NotificationController]
/// instance as the shell's `NotificationBadge` — marking a row read here
/// updates the bell's count immediately, with no separate fetch.
class NotificationListView extends StatefulWidget {
  final NotificationController controller;

  const NotificationListView({super.key, required this.controller});

  @override
  State<NotificationListView> createState() => _NotificationListViewState();
}

class _NotificationListViewState extends State<NotificationListView> {
  @override
  void initState() {
    super.initState();
    widget.controller.refresh();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final controller = widget.controller;

        if (controller.isLoading && controller.notifications.isEmpty) {
          return const SizedBox(
            height: 160,
            child: Center(child: CircularProgressIndicator()),
          );
        }
        if (controller.error != null && controller.notifications.isEmpty) {
          return SizedBox(
            height: 160,
            child: Center(child: ErrorBanner(message: controller.error!)),
          );
        }
        if (controller.notifications.isEmpty) {
          return const SizedBox(
            height: 160,
            child: Center(child: Text('Nema obavještenja.')),
          );
        }

        return RefreshIndicator(
          onRefresh: controller.refresh,
          child: ListView.separated(
            shrinkWrap: true,
            physics: const AlwaysScrollableScrollPhysics(),
            itemCount: controller.notifications.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, index) => _buildTile(controller.notifications[index]),
          ),
        );
      },
    );
  }

  Widget _buildTile(NotificationDto item) {
    return ListTile(
      leading: Icon(
        item.isRead ? Icons.notifications_none : Icons.notifications_active,
        color: item.isRead ? null : Theme.of(context).colorScheme.primary,
      ),
      title: Text(
        item.title,
        style: TextStyle(fontWeight: item.isRead ? FontWeight.normal : FontWeight.bold),
      ),
      subtitle: Text(item.body),
      trailing: Text(formatDayTime(item.createdAt)),
      onTap: item.isRead ? null : () => widget.controller.markRead(item.id),
    );
  }
}
