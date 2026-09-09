import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'package:enote_core/enote_core.dart';

/// SignalR client for `/hubs/notifications` (T35).
///
/// Primary (instant) path for notifications; the 30 s REST poll in
/// [NotificationController] stays the always-on fallback, so a failed hub
/// never breaks the badge — [start] logs and returns instead of throwing.
class NotificationHubClient {
  final String hubUrl;
  final Future<String> Function() tokenProvider;

  /// Refreshes the inbox list (called on every push; the hub payload has no
  /// `id`/`isRead`, so it cannot be inserted locally).
  VoidCallback onRefresh;

  /// Shows the foreground push UI (called after [onRefresh]).
  void Function(NotificationPushDto push) onPush;

  HubConnection? _connection;
  bool _starting = false;

  NotificationHubClient({
    required this.hubUrl,
    required this.tokenProvider,
    this.onRefresh = _noop,
    this.onPush = _noopPush,
  });

  static void _noop() {}

  static void _noopPush(NotificationPushDto _) {}

  /// Derives the hub URL from the API base URL the same way core
  /// `networkImageOrPlaceholder` derives the origin (01 G13).
  static String deriveHubUrl(String apiBaseUrl) {
    final uri = Uri.tryParse(apiBaseUrl);
    final origin = (uri != null && uri.hasScheme)
        ? uri.origin
        : apiBaseUrl.replaceAll(RegExp(r'/api/v\d+/?$'), '');
    return '$origin/hubs/notifications';
  }

  bool get isConnected =>
      _connection?.state == HubConnectionState.Connected;

  Future<void> start() async {
    if (_connection != null &&
        _connection!.state != HubConnectionState.Disconnected) {
      return;
    }
    if (_starting) return;
    _starting = true;
    try {
      final connection = HubConnectionBuilder()
          .withUrl(
            hubUrl,
            options: HttpConnectionOptions(
              accessTokenFactory: () => tokenProvider(),
            ),
          )
          .withAutomaticReconnect()
          .build();
      connection.on('ReceiveNotification', _handlePush);
      _connection = connection;
      await connection.start();
      debugPrint('SignalR hub started: $hubUrl');
    } catch (e) {
      debugPrint('SignalR hub start failed (polling fallback active): $e');
      _connection = null;
    } finally {
      _starting = false;
    }
  }

  Future<void> stop() async {
    final connection = _connection;
    _connection = null;
    if (connection == null) return;
    try {
      await connection.stop();
      debugPrint('SignalR hub stopped: $hubUrl');
    } catch (e) {
      debugPrint('SignalR hub stop failed: $e');
    }
  }

  void _handlePush(List<Object?>? args) {
    try {
      if (args == null || args.isEmpty) return;
      final raw = args[0];
      if (raw is! Map) return;
      final push = NotificationPushDto.fromJson(
        Map<String, dynamic>.from(raw),
      );
      debugPrint('SignalR push received: ${push.title}');
      onRefresh();
      onPush(push);
    } catch (e) {
      debugPrint('SignalR push parse failed: $e');
    }
  }

  void dispose() {
    unawaited(stop());
  }
}
