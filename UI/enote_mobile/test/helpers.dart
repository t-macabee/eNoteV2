import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';
import 'package:enote_test_support/enote_test_support.dart';
import 'package:signalr_netcore/signalr_client.dart';
export 'package:enote_test_support/enote_test_support.dart';

Map<String, dynamic> meJson({DateTime? paidUntil, bool hasPicture = false}) => {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {
    'id': 7,
    'firstName': 'Student',
    'lastName': 'Enote',
    'dateOfBirth': '2001-05-12T00:00:00',
    'membershipPaidUntil':
        (paidUntil ?? DateTime.utc(2027, 9, 9)).toIso8601String(),
  },
  'hasPicture': hasPicture,
};

/// Number of recorded requests matching `'METHOD /path'` (query ignored).
int countRequests(ScriptedClient client, String marker) =>
    client.requests.where((r) => '${r.method} ${r.url.path}' == marker).length;

Future<void> pumpPastDebounce(WidgetTester tester) => tester.pump(
  PagedFetchController.defaultSearchDebounce + const Duration(milliseconds: 100),
);

/// Hand-rolled [HubConnection] double: connects instantly, records lifecycles,
/// never touches the network.
class FakeHubConnection implements HubConnection {
  HubConnectionState _state = HubConnectionState.Disconnected;
  int startCalls = 0;
  int stopCalls = 0;

  @override
  HubConnectionState? get state => _state;

  @override
  Future<void>? start() async {
    startCalls++;
    _state = HubConnectionState.Connected;
  }

  @override
  Future<void> stop() async {
    stopCalls++;
    _state = HubConnectionState.Disconnected;
  }

  @override
  void on(String methodName, MethodInvocationFunc newMethod) {}

  @override
  void off(String methodName, {MethodInvocationFunc? method}) {}

  @override
  void onclose(ClosedCallback callback) {}

  @override
  void onreconnecting(ReconnectingCallback callback) {}

  @override
  void onreconnected(ReconnectedCallback callback) {}

  @override
  Stream<Object?> stream(String methodName, List<Object> args) =>
      const Stream.empty();

  @override
  StreamController<Object?> streamControllable(
    String methodName,
    List<Object> args,
  ) => StreamController<Object?>();

  @override
  Future<void> send(String methodName, {List<Object>? args}) async {}

  @override
  Future<Object?> invoke(String methodName, {List<Object>? args}) async =>
      null;

  @override
  Stream<HubConnectionState> get stateStream => const Stream.empty();

  @override
  String? get connectionId => 'fake';

  @override
  String? get baseUrl => 'http://localhost/hubs/notifications';

  @override
  set baseUrl(String? url) {}

  @override
  int serverTimeoutInMilliseconds = 30000;

  @override
  int keepAliveIntervalInMilliseconds = 15000;
}
