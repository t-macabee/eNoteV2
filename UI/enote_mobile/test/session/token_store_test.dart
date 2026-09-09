import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/session/token_store.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const channel = MethodChannel(
    'plugins.it_nomads.com/flutter_secure_storage',
  );
  final stored = <String, String>{};
  var failNext = false;

  setUp(() {
    stored.clear();
    failNext = false;
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          if (failNext) {
            throw PlatformException(code: 'ERROR');
          }
          final args = (call.arguments as Map).cast<String, dynamic>();
          switch (call.method) {
            case 'read':
              return stored[args['key'] as String];
            case 'write':
              stored[args['key'] as String] = args['value'] as String;
              return null;
            case 'delete':
              stored.remove(args['key'] as String);
              return null;
          }
          return null;
        });
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, null);
  });

  test('read returns the mirror before the persist completes', () async {
    final store = TokenStore();
    store.write('abc');
    expect(store.read(), 'abc');
    await Future<void>.delayed(Duration.zero);
    expect(stored[TokenStore.tokenKey], 'abc');
  });

  test('write null deletes', () async {
    stored[TokenStore.tokenKey] = 'abc';
    final store = TokenStore();
    await store.load();
    expect(store.read(), 'abc');
    store.write(null);
    expect(store.read(), isNull);
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);
    expect(stored.containsKey(TokenStore.tokenKey), isFalse);
  });

  test('a storage exception is logged, not thrown', () async {
    final store = TokenStore();
    failNext = true;
    await store.load();
    expect(store.read(), isNull);
    store.write('abc');
    expect(store.read(), 'abc');
    await Future<void>.delayed(Duration.zero);
  });
}
