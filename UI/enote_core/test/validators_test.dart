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
        equals('Lozinke se ne podudaraju.'),
      );
      expect(validate('Drugacij1!'), isNull);
    });

    test('empty confirmation still required', () {
      final validate = Validators.confirmPassword(() => 'Test1234!');
      expect(validate(''), equals('Potvrdite lozinku.'));
      expect(validate(null), equals('Potvrdite lozinku.'));
    });
  });

  group('password', () {
    test('accepts non-ASCII Bosnian letters', () {
      expect(Validators.password('Čšđ1234!'), isNull);
    });

    test('requires an uppercase letter by Unicode class', () {
      expect(
        Validators.password('čšđ1234!'),
        equals('Lozinka mora sadržavati najmanje jedno veliko slovo.'),
      );
    });

    test('short password reports znakova', () {
      expect(
        Validators.password('Ab1!'),
        equals('Lozinka mora imati najmanje 8 znakova.'),
      );
    });
  });

  group('nonNegativeDecimal', () {
    test('empty value says Vrijednost je obavezna.', () {
      expect(
        Validators.nonNegativeDecimal(''),
        equals('Vrijednost je obavezna.'),
      );
    });

    test('negative value says Vrijednost ne smije biti negativna.', () {
      expect(
        Validators.nonNegativeDecimal('-1'),
        equals('Vrijednost ne smije biti negativna.'),
      );
    });
  });
}
