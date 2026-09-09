import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

void main() {
  const jsonHeaders = {'content-type': 'application/json; charset=utf-8'};

  group('throwIfError', () {
    test('does not throw when status code is < 400', () {
      expect(
        () => throwIfError(http.Response('{"ok": true}', 200, headers: jsonHeaders)),
        returnsNormally,
      );
      expect(() => throwIfError(http.Response('', 204)), returnsNormally);
    });

    test('400 throws ApiException with mapped message from body', () {
      final response = http.Response(
        '{"status": 400, "message": "Neispravan ID korisnika."}',
        400,
        headers: jsonHeaders,
      );
      expect(
        () => throwIfError(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan ID korisnika.',
          ),
        ),
      );
    });

    test('400 without custom body message throws default mapped message', () {
      final response = http.Response('', 400);
      expect(
        () => throwIfError(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan zahtjev.',
          ),
        ),
      );
    });

    test('404 throws default mapped message', () {
      final response = http.Response('', 404);
      expect(
        () => throwIfError(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Resurs nije pronađen.',
          ),
        ),
      );
    });
  });

  group('decodeOrThrow', () {
    test('200 decodes JSON object into Map<String, dynamic>', () {
      final response = http.Response(
        '{"id": 42, "name": "Test"}',
        200,
        headers: jsonHeaders,
      );
      final decoded = decodeOrThrow(response);
      expect(decoded, {'id': 42, 'name': 'Test'});
    });

    test('400 throws ApiException with mapped message', () {
      final response = http.Response(
        '{"message": "Greška pri validaciji."}',
        400,
        headers: jsonHeaders,
      );
      expect(
        () => decodeOrThrow(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Greška pri validaciji.',
          ),
        ),
      );
    });

    test('empty body throws ApiException with parse-failure message', () {
      final emptyResponse = http.Response('', 200);
      expect(
        () => decodeOrThrow(emptyResponse),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan odgovor servera.',
          ),
        ),
      );

      final whitespaceResponse = http.Response('   ', 200);
      expect(
        () => decodeOrThrow(whitespaceResponse),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan odgovor servera.',
          ),
        ),
      );
    });

    test('non-JSON body throws ApiException with parse-failure message', () {
      final response = http.Response('<html>oops</html>', 200);
      expect(
        () => decodeOrThrow(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan odgovor servera.',
          ),
        ),
      );
    });

    test('JSON array body throws ApiException with parse-failure message', () {
      final response = http.Response('[{"id": 1}]', 200, headers: jsonHeaders);
      expect(
        () => decodeOrThrow(response),
        throwsA(
          isA<ApiException>().having(
            (e) => e.message,
            'message',
            'Neispravan odgovor servera.',
          ),
        ),
      );
    });
  });
}
