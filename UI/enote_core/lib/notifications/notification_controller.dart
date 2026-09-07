import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../api/api_client.dart';
import '../api/api_error_mapper.dart';
import '../api/api_response.dart';
import '../models/communication/communication_models.dart';

class NotificationController extends ChangeNotifier {
  final ApiClient apiClient;
  final String endpoint;
  final Duration pollInterval;

  List<NotificationDto> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  String? _error;
  Timer? _pollTimer;

  NotificationController({
    required this.apiClient,
    required this.endpoint,
    this.pollInterval = const Duration(seconds: 30),
  });

  List<NotificationDto> get notifications => List.unmodifiable(_notifications);
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  String? get error => _error;

  /// Starts refreshing the unread count on [pollInterval] and loads the
  /// first page immediately. Call once when the shell mounts.
  void startPolling() {
    _pollTimer?.cancel();
    refresh();
    _pollTimer = Timer.periodic(pollInterval, (_) => refreshUnreadCount());
  }

  void stopPolling() {
    _pollTimer?.cancel();
    _pollTimer = null;
  }

  Future<void> refresh({NotificationSearchObject? search}) async {
    _isLoading = true;
    _error = null;
    // Deferred: refresh() can be called synchronously from a widget's
    // didChangeDependencies/initState (e.g. MasterScreen.startPolling on
    // login), which runs mid-build. Notifying listeners before any `await`
    // would try to rebuild an ancestor Provider while the framework is still
    // building this frame ("setState() or markNeedsBuild() called during
    // build"). scheduleMicrotask pushes the notification past the end of
    // the current build/frame.
    scheduleMicrotask(notifyListeners);

    try {
      final response = await apiClient.get(
        endpoint,
        queryParams:
            (search ?? NotificationSearchObject(pageSize: 50)).toQueryMap(),
      );
      final data = decodeOrThrow(response);
      final items = (data['items'] as List<dynamic>? ?? []);
      _notifications = items
          .map((e) => NotificationDto.fromJson(Map<String, dynamic>.from(e)))
          .toList();
      _unreadCount = _notifications.where((n) => !n.isRead).length;
    } catch (e) {
      _error = userMessage(e);
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Cheaper than [refresh] — used by the polling timer so it doesn't
  /// re-fetch the whole list every tick.
  Future<void> refreshUnreadCount() async {
    try {
      final response = await apiClient.get('$endpoint/unread-count');
      if (response.statusCode >= 400) return;
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      _unreadCount = NotificationUnreadCountDto.fromJson(data).unreadCount;
      notifyListeners();
    } catch (_) {
      // Silent: the next poll tick retries.
    }
  }

  Future<void> markRead(int id) async {
    final response = await apiClient.patch('$endpoint/$id/read');
    throwIfError(response);

    final index = _notifications.indexWhere((n) => n.id == id);
    if (index != -1 && !_notifications[index].isRead) {
      _notifications[index] = _markedRead(_notifications[index]);
      _unreadCount = _notifications.where((n) => !n.isRead).length;
      notifyListeners();
    }
  }

  Future<void> markAllRead() async {
    final response = await apiClient.patch('$endpoint/read-all');
    throwIfError(response);

    _notifications = _notifications.map(_markedRead).toList();
    _unreadCount = 0;
    notifyListeners();
  }

  NotificationDto _markedRead(NotificationDto n) => NotificationDto(
        id: n.id,
        rentalId: n.rentalId,
        lectureId: n.lectureId,
        submissionId: n.submissionId,
        title: n.title,
        body: n.body,
        isRead: true,
        createdAt: n.createdAt,
      );

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }
}
