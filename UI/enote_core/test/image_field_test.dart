import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

final Uint8List _validPngBytes = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
);

final PickedImage _picked = PickedImage(
  bytes: _validPngBytes,
  fileName: 'photo.png',
  contentType: 'image/png',
);

void main() {
  testWidgets('a refused upload removes the local preview', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ImageField(
            alignment: Alignment.center,
            imagePicker: () async => _picked,
            onUpload: (bytes, fileName, contentType) async => null,
          ),
        ),
      ),
    );

    await tester.tap(find.byType(GestureDetector));
    await tester.pumpAndSettle();

    expect(find.byType(Image), findsNothing);
  });

  testWidgets('an accepted upload keeps the preview and sends the real type', (
    tester,
  ) async {
    String? sentName;
    String? sentType;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ImageField(
            alignment: Alignment.center,
            imagePicker: () async => _picked,
            onUpload: (bytes, fileName, contentType) async {
              sentName = fileName;
              sentType = contentType;
              return '/api/v1/uploads/instruments/new.png';
            },
          ),
        ),
      ),
    );

    await tester.tap(find.byType(GestureDetector));
    await tester.pump();
    await tester.pump();

    expect(sentName, 'photo.png');
    expect(sentType, 'image/png');
    expect(find.byType(Image), findsOneWidget);
  });

  group('pickedImageOrNull', () {
    final bytes = Uint8List(4);

    test('maps each allowed extension to its type, in any case', () {
      expect(pickedImageOrNull(bytes, 'a.jpg')!.contentType, 'image/jpeg');
      expect(pickedImageOrNull(bytes, 'a.JPEG')!.contentType, 'image/jpeg');
      expect(pickedImageOrNull(bytes, 'a.png')!.contentType, 'image/png');
      expect(pickedImageOrNull(bytes, 'a.WebP')!.contentType, 'image/webp');
    });

    test('refuses other extensions', () {
      expect(pickedImageOrNull(bytes, 'a.gif'), isNull);
      expect(pickedImageOrNull(bytes, 'a.bmp'), isNull);
      expect(pickedImageOrNull(bytes, 'noextension'), isNull);
    });

    test('accepts exactly 5242880 bytes and refuses one more', () {
      expect(pickedImageOrNull(Uint8List(5242880), 'a.png'), isNotNull);
      expect(pickedImageOrNull(Uint8List(5242881), 'a.png'), isNull);
    });
  });

  testWidgets('onUpload completes after dispose -> no exception',
      (WidgetTester tester) async {
    final completer = Completer<String?>();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ImageField(
            alignment: Alignment.center,
            imagePicker: () async => _picked,
            onUpload: (bytes, fileName, contentType) => completer.future,
          ),
        ),
      ),
    );

    await tester.tap(find.byType(GestureDetector));
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    await tester.pumpWidget(const SizedBox());

    completer.complete('http://example.com/image.jpg');
    await tester.pump();

    expect(tester.takeException(), isNull);
  });

  testWidgets('onUpload throws -> spinner cleared, error propagates',
      (WidgetTester tester) async {
    Object? capturedError;

    await runZonedGuarded(() async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ImageField(
              alignment: Alignment.center,
              imagePicker: () async => _picked,
              onUpload: (bytes, fileName, contentType) async {
                throw Exception('upload failed');
              },
            ),
          ),
        ),
      );

      await tester.tap(find.byType(GestureDetector));
      await tester.pump();
      await tester.pump();
    }, (error, stack) {
      capturedError = error;
    });

    expect(capturedError, isA<Exception>());
    expect(capturedError.toString(), contains('upload failed'));
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });
}
