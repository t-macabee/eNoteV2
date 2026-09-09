import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/api/api_error_mapper.dart';

void main() {
  group('ApiErrorMapper validation mapping', () {
    test('one field with one message', () {
      final body = jsonEncode({
        'type': 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        'title': 'One or more validation errors occurred.',
        'status': 400,
        'errors': {
          'Password': ['Lozinka mora imati najmanje 8 znakova.']
        },
      });
      expect(ApiErrorMapper.mapError(400, body),
          'Lozinka mora imati najmanje 8 znakova.');
    });

    test('two fields with three messages joined by newline', () {
      final body = jsonEncode({
        'status': 400,
        'errors': {
          'Username': ['Korisničko ime je obavezno.', 'Druga greška.'],
          'Email': ['Unesite važeću email adresu.'],
        },
      });
      expect(ApiErrorMapper.mapError(400, body),
          'Korisničko ime je obavezno.\nDruga greška.\nUnesite važeću email adresu.');
    });

    test('title only on 400', () {
      final body =
          jsonEncode({'title': 'One or more validation errors occurred.'});
      expect(ApiErrorMapper.mapError(400, body),
          'One or more validation errors occurred.');
      expect(ApiErrorMapper.mapError(404, body),
          'Resurs nije pronađen.');
    });

    test('message still wins over errors', () {
      final body = jsonEncode({
        'message': 'Neispravan ID korisnika.',
        'errors': {
          'Password': ['Lozinka mora imati najmanje 8 znakova.']
        },
      });
      expect(
          ApiErrorMapper.mapError(400, body), 'Neispravan ID korisnika.');
    });

    test('401 default spells istekla', () {
      expect(ApiErrorMapper.mapError(401, ''), contains('istekla'));
    });

    test('ApiError.fromJson parses errors map', () {
      final error = ApiError.fromJson({
        'status': 400,
        'errors': {
          'Password': ['a', 'b']
        },
      });
      expect(error.errors, {
        'Password': ['a', 'b']
      });
    });
  });
}
