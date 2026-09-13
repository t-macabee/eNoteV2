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

  Future<void> bootstrap() async {
    _bootstrapError = null;
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
    try {
      await apiClient.post('auth/logout');
    } catch (_) {}
    await authState.logout();
    notifications.stopPolling();
    onStopRealtime?.call();
  }
}
