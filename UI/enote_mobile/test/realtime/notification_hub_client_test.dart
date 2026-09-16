import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'package:enote_mobile/realtime/notification_hub_client.dart';

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

void main() {
  test('start() twice with a different token rebuilds the connection',
      () async {
    var token = 'token-a';
    var factoryCalls = 0;
    final client = NotificationHubClient(
      hubUrl: 'http://localhost/hubs/notifications',
      tokenProvider: () async => token,
      connectionFactory: () {
        factoryCalls++;
        return FakeHubConnection();
      },
    );

    await client.start();
    expect(factoryCalls, 1);

    token = 'token-b';
    await client.start();
    expect(factoryCalls, 2);
  });

  test('start() twice with the same token does not rebuild', () async {
    var factoryCalls = 0;
    final client = NotificationHubClient(
      hubUrl: 'http://localhost/hubs/notifications',
      tokenProvider: () async => 'token-a',
      connectionFactory: () {
        factoryCalls++;
        return FakeHubConnection();
      },
    );

    await client.start();
    await client.start();
    expect(factoryCalls, 1);
  });
}
