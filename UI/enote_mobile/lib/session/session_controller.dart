import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/membership/membership.dart' as membership;

class SessionController extends ChangeNotifier {
  final ApiClient apiClient;
  final AuthState authState;
  final NotificationController notifications;
  final VoidCallback? onStopRealtime;

  static bool _sessionExpired = false;

  static void markSessionExpired() {
    _sessionExpired = true;
  }

  /// Stops realtime delivery when the 401 path in `main.dart` fires. Wired by
  /// the app-level provider to the same closure passed as [onStopRealtime]
  /// (which [logoutAndRevoke] uses), because `main.dart` has no instance and
  /// must not build a second hub client.
  static VoidCallback? stopRealtime;

  UserProfileResponse? _profile;
  int _pictureVersion = 0;
  Object? _bootstrapError;

  SessionController({
    required this.apiClient,
    required this.authState,
    required this.notifications,
    this.onStopRealtime,
  });

  UserProfileResponse? get profile => _profile;
  Object? get bootstrapError => _bootstrapError;

  /// Read path for the profile picture URL cache buster. Bumped on every
  /// successful [reloadProfile] so the picture URL carries a fresh `?v=`
  /// after an upload/delete and the new bytes are fetched (the URL is
  /// cacheable, so the version is what forces a reload).
  int get pictureVersion => _pictureVersion;

  DateTime? get membershipPaidUntil => profile?.profile.membershipPaidUntil;

  /// The membership is active through the end of the expiry **day**
  /// (inclusive boundary, UTC): `false` when no expiry is set; otherwise
  /// `true` while [membershipPaidUntil]'s UTC calendar day is not before the
  /// current UTC calendar day.
  bool get isMembershipActive =>
      membership.isMembershipActive(membershipPaidUntil);

  bool consumeSessionExpired() {
    final value = _sessionExpired;
    _sessionExpired = false;
    return value;
  }

  /// Drops the cached user so nothing from a previous session is readable —
  /// on logout, and on every fresh [bootstrap] (the 401 path in `main.dart`
  /// has no instance; the next `RootShell` mount bootstraps and clears here).
  void _clearProfile() {
    _profile = null;
    _bootstrapError = null;
    _pictureVersion = 0;
  }

  Future<void> bootstrap() async {
    _clearProfile();
    try {
      final results = await Future.wait<dynamic>([
        apiClient.get('users/me'),
        notifications.refresh(),
      ]);
      _profile = UserProfileResponse.fromJson(
        decodeOrThrow(results[0] as http.Response),
      );
    } catch (e) {
      _bootstrapError = e;
    }
    notifyListeners();
  }

  Future<void> reloadProfile() async {
    final response = await apiClient.get('users/me');
    _profile = UserProfileResponse.fromJson(decodeOrThrow(response));
    _pictureVersion++;
    _bootstrapError = null;
    notifyListeners();
  }

  Future<void> logoutAndRevoke() async {
    _clearProfile();
    try {
      await apiClient.post('auth/logout');
    } catch (_) {}
    await authState.logout();
    notifications.stopPolling();
    onStopRealtime?.call();
  }

  @override
  void dispose() {
    if (identical(stopRealtime, onStopRealtime)) stopRealtime = null;
    super.dispose();
  }
}
