import 'dart:async';

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
  int _page = 1;
  int _pageSize = 50;
  bool _hasMore = false;
  bool _isLoadingMore = false;

  NotificationController({
    required this.apiClient,
    required this.endpoint,
    this.pollInterval = const Duration(seconds: 30),
  });

  List<NotificationDto> get notifications => List.unmodifiable(_notifications);
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get hasMore => _hasMore;

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
    scheduleMicrotask(notifyListeners);

    try {
      final pageSize = search?.pageSize ?? 50;
      final unreadFuture = refreshUnreadCount();
      final response = await apiClient.get(
        endpoint,
        queryParams: (search ?? NotificationSearchObject(pageSize: 50))
            .toQueryMap()
          ..['page'] = 1
          ..['pageSize'] = pageSize,
      );
      final data = decodeOrThrow(response);
      final items = (data['items'] as List<dynamic>? ?? []);
      _notifications = items
          .map((e) => NotificationDto.fromJson(Map<String, dynamic>.from(e)))
          .toList();
      _page = 1;
      _pageSize = pageSize;
      _hasMore = _notifications.length == pageSize;
      await unreadFuture;
    } catch (e) {
      _error = userMessage(e);
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMore() async {
    if (_isLoadingMore) return;
    _isLoadingMore = true;
    try {
      final nextPage = _page + 1;
      final response = await apiClient.get(
        endpoint,
        queryParams: {'page': nextPage, 'pageSize': _pageSize},
      );
      final data = decodeOrThrow(response);
      final items = (data['items'] as List<dynamic>? ?? []);
      final parsed = items
          .map((e) => NotificationDto.fromJson(Map<String, dynamic>.from(e)))
          .toList();
      _notifications = [..._notifications, ...parsed];
      _page = nextPage;
      _hasMore = parsed.length == _pageSize;
      notifyListeners();
    } finally {
      _isLoadingMore = false;
    }
  }

  Future<void> refreshUnreadCount() async {
    try {
      final response = await apiClient.get('$endpoint/unread-count');
      final data = decodeOrThrow(response);
      _unreadCount = NotificationUnreadCountDto.fromJson(data).unreadCount;
      notifyListeners();
    } catch (e) {
      debugPrint('Unread count refresh failed: $e');
    }
  }

  Future<void> markRead(int id) async {
    final response = await apiClient.patch('$endpoint/$id/read');
    throwIfError(response);

    final index = _notifications.indexWhere((n) => n.id == id);
    if (index != -1 && !_notifications[index].isRead) {
      _notifications[index] = _markedRead(_notifications[index]);
      if (_unreadCount > 0) {
        _unreadCount--;
      }
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
        announcementId: n.announcementId,
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
