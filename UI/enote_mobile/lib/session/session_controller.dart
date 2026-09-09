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
  int _pictureVersion = 0;

  SessionController({
    required this.apiClient,
    required this.authState,
    required this.notifications,
    this.onStopRealtime,
  });

  UserProfileResponse? get profile => _profile;

  /// Read path for the profile picture URL cache buster. Bumped on every
  /// successful [reloadProfile] so the picture URL carries a fresh `?v=`
  /// after an upload/delete and the new bytes are fetched (the URL is
  /// cacheable, so the version is what forces a reload).
  int get pictureVersion => _pictureVersion;

  DateTime? get membershipPaidUntil => profile?.profile.membershipPaidUntil;

  /// The membership is active through the end of the expiry **day**
  /// (inclusive boundary, local time): `false` when no expiry is set;
  /// otherwise `true` while now is before the start of the day after
  /// [membershipPaidUntil]'s calendar day.
  bool get isMembershipActive {
    final until = membershipPaidUntil;
    if (until == null) return false;
    final local = until.toLocal();
    final endOfDay = DateTime(
      local.year,
      local.month,
      local.day,
    ).add(const Duration(days: 1));
    return DateTime.now().isBefore(endOfDay);
  }

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
    _pictureVersion++;
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
