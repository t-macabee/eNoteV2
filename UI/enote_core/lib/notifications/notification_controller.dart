import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';

import '../api/api_client.dart';
import '../api/api_error_mapper.dart';
import '../models/communication/communication_models.dart';

/// Shared state behind the shell's notification bell and the full
/// notification screen. One controller instance drives both widgets, so
/// marking something read from the list updates the bell's count with no
/// separate fetch, and the two never fall out of sync.
///
/// Polls on a timer for now — swapping the polling loop for a SignalR push
/// subscription later (see the Flutter plan's risk register) can happen
/// entirely inside this class without either widget changing.
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
    notifyListeners();

    try {
      final response = await apiClient.get(
        endpoint,
        queryParams:
            (search ?? NotificationSearchObject(pageSize: 50)).toQueryMap(),
      );
      if (response.statusCode >= 400) {
        throw Exception(ApiErrorMapper.mapError(response.statusCode, response.body));
      }
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      final items = (data['items'] as List<dynamic>? ?? []);
      _notifications = items
          .map((e) => NotificationDto.fromJson(Map<String, dynamic>.from(e)))
          .toList();
      _unreadCount = _notifications.where((n) => !n.isRead).length;
    } catch (e) {
      _error = e.toString();
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
    if (response.statusCode >= 400) {
      throw Exception(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final index = _notifications.indexWhere((n) => n.id == id);
    if (index != -1 && !_notifications[index].isRead) {
      _notifications[index] = _markedRead(_notifications[index]);
      _unreadCount = _notifications.where((n) => !n.isRead).length;
      notifyListeners();
    }
  }

  Future<void> markAllRead() async {
    final response = await apiClient.patch('$endpoint/read-all');
    if (response.statusCode >= 400) {
      throw Exception(ApiErrorMapper.mapError(response.statusCode, response.body));
    }

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
