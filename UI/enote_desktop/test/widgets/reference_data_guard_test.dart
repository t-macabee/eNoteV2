import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/reference_data_guard.dart';

const _message = 'Prvo dodajte barem jedan grad u dijelu Referentni podaci.';

Future<bool?> _runGuard(
  WidgetTester tester,
  Future<PagedResult<String>> Function() fetchFirstPage,
) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  bool? result;
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () async {
              result = await ensureReferenceData(
                context,
                fetchFirstPage: fetchFirstPage,
                message: _message,
              );
            },
            child: const Text('Pokreni'),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('Pokreni'));
  await tester.pumpAndSettle();
  return result;
}

void main() {
  testWidgets('returns true when the list has an item and shows no message', (
    WidgetTester tester,
  ) async {
    final result = await _runGuard(
      tester,
      () async => PagedResult<String>(
        items: const ['Sarajevo'],
        page: 1,
        pageSize: 1,
        totalCount: 1,
      ),
    );

    expect(result, isTrue);
    expect(find.text(_message), findsNothing);
  });

  testWidgets('returns false and shows the message when the list is empty', (
    WidgetTester tester,
  ) async {
    final result = await _runGuard(
      tester,
      () async => PagedResult<String>(
        items: const [],
        page: 1,
        pageSize: 1,
        totalCount: 0,
      ),
    );

    expect(result, isFalse);
    expect(find.text(_message), findsOneWidget);
  });

  testWidgets('returns true when the request fails', (
    WidgetTester tester,
  ) async {
    final result = await _runGuard(
      tester,
      () async => throw Exception('network'),
    );

    expect(result, isTrue);
    expect(find.text(_message), findsNothing);
  });
}
