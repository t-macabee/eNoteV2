import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('Validators', () {
    test('email rejects invalid addresses', () {
      expect(Validators.email('not-an-email'), isNotNull);
      expect(Validators.email('user@example.com'), isNull);
    });

    test('required rejects blank values', () {
      final validator = Validators.required('Ime');
      expect(validator(''), isNotNull);
      expect(validator('Ana'), isNull);
    });
  });

  group('userMessage', () {
    test('passes an ApiException\'s mapped message through unchanged', () {
      final apiException = ApiException('Nemate pristup ovom resursu.');
      expect(userMessage(apiException), 'Nemate pristup ovom resursu.');
    });

    test('collapses any other error to the generic Bosnian fallback', () {
      expect(
        userMessage(const SocketException('Connection refused')),
        'Nije moguće povezati se sa serverom. Pokušajte ponovo.',
      );
      expect(
        userMessage(const FormatException('unexpected token')),
        'Nije moguće povezati se sa serverom. Pokušajte ponovo.',
      );
    });
  });

  group('PagedResult', () {
    test('computes totalPages and hasNext', () {
      final result = PagedResult<int>(
        items: [1, 2, 3],
        page: 1,
        pageSize: 3,
        totalCount: 10,
      );
      expect(result.totalPages, 4);
      expect(result.hasNext, isTrue);
      expect(result.hasPrevious, isFalse);
    });
  });
}
