import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

final Uint8List _validPngBytes = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
);

void main() {
  testWidgets('onUpload completes after dispose -> no exception',
      (WidgetTester tester) async {
    final completer = Completer<String?>();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ImageField(
            alignment: Alignment.center,
            imagePicker: () async => _validPngBytes,
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
              imagePicker: () async => _validPngBytes,
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
