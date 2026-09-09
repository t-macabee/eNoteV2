import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'helpers.dart';

void main() {
  test('a RecordingHttpClient and a Student JWT construct', () {
    final client = RecordingHttpClient();
    expect(client.requests, isEmpty);
    final auth = AuthState(tokenReader: () => fakeJwt());
    expect(auth.isAuthenticated, isTrue);
    expect(auth.hasRole('Student'), isTrue);
  });
}
