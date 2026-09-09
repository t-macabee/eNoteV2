import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

class SessionController extends ChangeNotifier {
  final ApiClient apiClient;
  final AuthState authState;
  final NotificationController notifications;
  final VoidCallback? onStopRealtime;

  static bool _sessionExpired = false;

  static void markSessionExpired() {
    _sessionExpired = true;
  }

  UserProfileResponse? _profile;

  SessionController({
    required this.apiClient,
    required this.authState,
    required this.notifications,
    this.onStopRealtime,
  });

  UserProfileResponse? get profile => _profile;

  bool consumeSessionExpired() {
    final value = _sessionExpired;
    _sessionExpired = false;
    return value;
  }

  Future<void> bootstrap() async {
    final results = await Future.wait<dynamic>([
      apiClient.get('users/me'),
      notifications.refreshUnreadCount(),
      notifications.refresh(),
    ]);
    _profile = UserProfileResponse.fromJson(
      decodeOrThrow(results[0] as http.Response),
    );
    notifyListeners();
  }

  Future<void> reloadProfile() async {
    final response = await apiClient.get('users/me');
    _profile = UserProfileResponse.fromJson(decodeOrThrow(response));
    notifyListeners();
  }

  Future<void> logoutAndRevoke() async {
    try {
      await apiClient.post('auth/logout');
    } catch (_) {}
    await authState.logout();
    notifications.stopPolling();
    onStopRealtime?.call();
  }
}
