import 'package:flutter/material.dart';

import '../api/api_error_mapper.dart';
import '../models/communication/communication_models.dart';
import '../notifications/notification_controller.dart';
import '../theme/app_tokens.dart';
import 'error_banner.dart';
import 'notification_tile.dart';

class NotificationListView extends StatefulWidget {
  final NotificationController controller;

  const NotificationListView({super.key, required this.controller});

  @override
  State<NotificationListView> createState() => _NotificationListViewState();
}

class _NotificationListViewState extends State<NotificationListView> {
  static const double _stateHeight = 160;

  @override
  void initState() {
    super.initState();
    widget.controller.refresh();
  }

  Future<void> _loadMore(NotificationController controller) async {
    try {
      await controller.loadMore();
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        final controller = widget.controller;

        if (controller.isLoading && controller.notifications.isEmpty) {
          return const SizedBox(
            height: _stateHeight,
            child: Center(child: CircularProgressIndicator()),
          );
        }
        if (controller.error != null && controller.notifications.isEmpty) {
          return SizedBox(
            height: _stateHeight,
            child: Center(child: ErrorBanner(message: controller.error!)),
          );
        }
        if (controller.notifications.isEmpty) {
          return const SizedBox(
            height: _stateHeight,
            child: Center(child: Text('Nema obavještenja.')),
          );
        }

        return RefreshIndicator(
          onRefresh: controller.refresh,
          child: ListView.separated(
            shrinkWrap: true,
            physics: const AlwaysScrollableScrollPhysics(),
            itemCount:
                controller.notifications.length + (controller.hasMore ? 1 : 0),
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, index) {
              if (index >= controller.notifications.length) {
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
                  child: Center(
                    child: OutlinedButton(
                      onPressed: () => _loadMore(controller),
                      child: const Text('Učitaj još'),
                    ),
                  ),
                );
              }
              return _buildTile(controller.notifications[index]);
            },
          ),
        );
      },
    );
  }

  Widget _buildTile(NotificationDto item) {
    return NotificationTile(
      notification: item,
      onTap: item.isRead ? null : () => widget.controller.markRead(item.id),
    );
  }
}
