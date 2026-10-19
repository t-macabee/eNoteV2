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

  int get pictureVersion => _pictureVersion;

  DateTime? get membershipPaidUntil => profile?.profile.membershipPaidUntil;

  bool get isMembershipActive =>
      membership.isMembershipActive(membershipPaidUntil);

  bool consumeSessionExpired() {
    final value = _sessionExpired;
    _sessionExpired = false;
    return value;
  }

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
    await authState.logout(revoke: true);
    notifications.stopPolling();
    onStopRealtime?.call();
  }

  @override
  void dispose() {
    if (identical(stopRealtime, onStopRealtime)) stopRealtime = null;
    super.dispose();
  }
}
