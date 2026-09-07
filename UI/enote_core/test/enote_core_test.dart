import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

class _DummyHttpClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(const Stream.empty(), 200);
  }
}

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

  group('networkImageOrPlaceholder', () {
    testWidgets('null or empty url renders placeholder', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: networkImageOrPlaceholder(
              null,
              null,
              size: 40,
              borderRadius: 4,
              placeholder: () => const Text('placeholder'),
            ),
          ),
        ),
      );

      expect(find.text('placeholder'), findsOneWidget);
    });

    testWidgets('relative path resolves against baseUrl origin without duplicate api prefix',
        (WidgetTester tester) async {
      final authState = AuthState(baseUrl: 'http://localhost:5059/api/v1/');
      final apiClient = ApiClient(
        baseUrl: 'http://localhost:5059/api/v1/',
        authState: authState,
        httpClient: _DummyHttpClient(),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: networkImageOrPlaceholder(
              '/api/v1/uploads/instruments/guitar.jpg',
              apiClient,
              size: 40,
              borderRadius: 4,
              placeholder: () => const Text('placeholder'),
            ),
          ),
        ),
      );

      final imageFinder = find.byType(Image);
      expect(imageFinder, findsOneWidget);
      final image = tester.widget<Image>(imageFinder);
      final provider = image.image as NetworkImage;
      expect(provider.url, 'http://localhost:5059/api/v1/uploads/instruments/guitar.jpg');
    });

    testWidgets('absolute url renders directly', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: networkImageOrPlaceholder(
              'https://example.com/photo.png',
              null,
              size: 40,
              borderRadius: 4,
              placeholder: () => const Text('placeholder'),
            ),
          ),
        ),
      );

      final imageFinder = find.byType(Image);
      expect(imageFinder, findsOneWidget);
      final image = tester.widget<Image>(imageFinder);
      final provider = image.image as NetworkImage;
      expect(provider.url, 'https://example.com/photo.png');
    });
  });
}
