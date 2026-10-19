import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/realtime/notification_hub_client.dart';

import '../helpers.dart';

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
