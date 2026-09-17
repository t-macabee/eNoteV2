import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/validators/validators.dart';

void main() {
  group('confirmPassword live getter', () {
    test('F4-09: reads the current password on every call, not a snapshot',
        () {
      var current = 'Test1234!';
      final validate = Validators.confirmPassword(() => current);

      expect(validate('Test1234!'), isNull);

      current = 'Drugacij1!';
      expect(
        validate('Test1234!'),
        equals('Lozinka i potvrda se ne poklapaju.'),
      );
      expect(validate('Drugacij1!'), isNull);
    });

    test('empty confirmation still required', () {
      final validate = Validators.confirmPassword(() => 'Test1234!');
      expect(validate(''), equals('Potvrdite lozinku.'));
      expect(validate(null), equals('Potvrdite lozinku.'));
    });
  });
}
