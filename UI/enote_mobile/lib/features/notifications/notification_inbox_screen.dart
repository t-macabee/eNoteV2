import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/root_shell.dart';
import '../../theme/app_theme.dart';
import '../../widgets/async_state_view.dart';

/// S29 — notification inbox on the root navigator.
///
/// Own tiles over the shared [NotificationController] (core
/// `NotificationListView` is not used, 02 §7): unread rows render a filled
/// primary dot with a bold title, read rows a hollow dot with normal weight.
/// Tapping an unread row PATCHes `…/read` first and only then routes by
/// payload; tapping a read row routes directly.
class NotificationInboxScreen extends StatefulWidget {
  const NotificationInboxScreen({super.key});

  @override
  State<NotificationInboxScreen> createState() =>
      _NotificationInboxScreenState();
}

class _NotificationInboxScreenState extends State<NotificationInboxScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) context.read<NotificationController>().refresh();
    });
  }

  Future<void> _markAllRead(NotificationController controller) async {
    try {
      await controller.markAllRead();
      await controller.refresh();
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
    }
  }

  Future<void> _onTap(
    BuildContext context,
    NotificationController controller,
    NotificationDto notification,
  ) async {
    if (!notification.isRead) {
      try {
        await controller.markRead(notification.id);
      } catch (e) {
        if (context.mounted) {
          ErrorBanner.show(context, message: userMessage(e));
        }
        return;
      }
    }
    if (!context.mounted) return;
    Navigator.of(context, rootNavigator: true).pop();
    RootShell.routeNotification(notification);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Obavještenja'),
        actions: [
          Consumer<NotificationController>(
            builder: (context, controller, _) {
              if (controller.unreadCount == 0) {
                return const Tooltip(
                  message: 'Nema nepročitanih',
                  child: TextButton(
                    onPressed: null,
                    child: Text('Označi sve kao pročitano'),
                  ),
                );
              }
              return TextButton(
                onPressed: () => _markAllRead(controller),
                child: const Text('Označi sve kao pročitano'),
              );
            },
          ),
        ],
      ),
      body: Consumer<NotificationController>(
        builder: (context, controller, _) {
          if (controller.error != null && controller.notifications.isEmpty) {
            return AsyncStateView(
              isLoading: false,
              error: controller.error,
              onRetry: controller.refresh,
              child: const SizedBox.shrink(),
            );
          }
          if (controller.isLoading && controller.notifications.isEmpty) {
            return const Center(child: CircularProgressIndicator());
          }
          if (!controller.isLoading && controller.notifications.isEmpty) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.notifications_outlined,
                      size: 48,
                      color: AppTheme.textTertiary,
                    ),
                    SizedBox(height: 12),
                    Text(
                      'Nema obavještenja.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: AppTheme.textSecondary),
                    ),
                  ],
                ),
              ),
            );
          }
          return RefreshIndicator(
            onRefresh: controller.refresh,
            child: ListView.separated(
              physics: const AlwaysScrollableScrollPhysics(),
              itemCount: controller.notifications.length +
                  (controller.hasMore ? 1 : 0),
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) {
                if (index >= controller.notifications.length) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: Center(
                      child: OutlinedButton(
                        onPressed: controller.loadMore,
                        child: const Text('Učitaj još'),
                      ),
                    ),
                  );
                }
                final notification = controller.notifications[index];
                return _NotificationTile(
                  notification: notification,
                  onTap: () => _onTap(context, controller, notification),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

class _NotificationTile extends StatelessWidget {
  final NotificationDto notification;
  final VoidCallback onTap;

  const _NotificationTile({required this.notification, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final unread = !notification.isRead;
    return ListTile(
      onTap: onTap,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      leading: Container(
        width: 8,
        height: 8,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: unread ? AppTheme.primary : Colors.transparent,
          border: unread
              ? null
              : Border.all(color: AppTheme.textSecondary, width: 1.5),
        ),
      ),
      title: Row(
        children: [
          Expanded(
            child: Text(
              notification.title,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontWeight: unread ? FontWeight.bold : FontWeight.normal,
                color: unread ? AppTheme.textPrimary : AppTheme.textSecondary,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            formatDayTime(notification.createdAt),
            style: TextStyle(
              fontSize: 12,
              color: unread ? AppTheme.textSecondary : AppTheme.textTertiary,
            ),
          ),
        ],
      ),
      subtitle: Text(
        notification.body,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: unread ? AppTheme.textPrimary : AppTheme.textSecondary,
        ),
      ),
    );
  }
}
