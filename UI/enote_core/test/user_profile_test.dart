import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('UserProfile.fromJson', () {
    test('nested profile shape parses', () {
      final profile = UserProfile.fromJson({
        'profile': {'id': 7, 'firstName': 'Ana', 'storeName': 'Sarajevo'},
      });

      expect(profile.id, 7);
      expect(profile.firstName, 'Ana');
      expect(profile.storeName, 'Sarajevo');
    });

    test('flat shape parses', () {
      final profile = UserProfile.fromJson(
          {'id': 7, 'firstName': 'Ana', 'storeName': 'Sarajevo'});

      expect(profile.id, 7);
      expect(profile.firstName, 'Ana');
      expect(profile.storeName, 'Sarajevo');
    });
  });
}
