import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../shell/root_shell.dart';
import '../../widgets/async_state_view.dart';
import '../../widgets/load_more_footer.dart';

class NotificationInboxScreen extends StatefulWidget {
  const NotificationInboxScreen({super.key});

  @override
  State<NotificationInboxScreen> createState() =>
      _NotificationInboxScreenState();
}

class _NotificationInboxScreenState extends State<NotificationInboxScreen> {
  bool _loadingMore = false;
  Object? _loadMoreError;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) context.read<NotificationController>().refresh();
    });
  }

  Future<void> _refresh(NotificationController controller) async {
    setState(() => _loadMoreError = null);
    await controller.refresh();
  }

  Future<void> _loadMore(NotificationController controller) async {
    if (_loadingMore) return;
    setState(() {
      _loadingMore = true;
      _loadMoreError = null;
    });
    try {
      await controller.loadMore();
    } catch (e) {
      if (mounted) setState(() => _loadMoreError = e);
    } finally {
      if (mounted) setState(() => _loadingMore = false);
    }
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
      appBar: AppBar(title: const Text('Obavještenja')),
      body: Consumer<NotificationController>(
        builder: (context, controller, _) {
          final notifications = controller.notifications;
          return RefreshIndicator(
            onRefresh: () => _refresh(controller),
            child: notifications.isEmpty
                ? ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      AsyncStateView(
                        isLoading: controller.isLoading,
                        error: controller.error,
                        isEmpty: true,
                        emptyMessage: 'Nema obavještenja.',
                        emptyIcon: Icons.notifications_outlined,
                        onRetry: () => _refresh(controller),
                        child: const SizedBox.shrink(),
                      ),
                    ],
                  )
                : _buildList(context, controller, notifications),
          );
        },
      ),
    );
  }

  Widget _buildList(
    BuildContext context,
    NotificationController controller,
    List<NotificationDto> notifications,
  ) {
    final showMarkAll = controller.unreadCount > 0;
    final itemCount = notifications.length + 1 + (showMarkAll ? 1 : 0);
    return ListView.separated(
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: itemCount,
      separatorBuilder: (_, index) => index < itemCount - 1
          ? const Divider(height: 1)
          : const SizedBox.shrink(),
      itemBuilder: (context, index) {
        final itemIndex = showMarkAll ? index - 1 : index;
        if (showMarkAll && index == 0) {
          return Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg,
              AppSpacing.sm,
              AppSpacing.lg,
              0,
            ),
            child: Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: () => _markAllRead(controller),
                child: const Text('Označi sve kao pročitano'),
              ),
            ),
          );
        }
        if (itemIndex == notifications.length) {
          return LoadMoreFooter(
            hasMore: controller.hasMore,
            isLoading: _loadingMore,
            error: _loadMoreError,
            onLoadMore: () => _loadMore(controller),
          );
        }
        final notification = notifications[itemIndex];
        return NotificationTile(
          notification: notification,
          onTap: () => _onTap(context, controller, notification),
        );
      },
    );
  }
}
