import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

class _DialogHost extends StatelessWidget {
  const _DialogHost({required this.onResult});

  final void Function(String?) onResult;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Builder(
        builder: (context) => ElevatedButton(
          onPressed: () async =>
              onResult(await promptForReason(context, title: 'Odbij')),
          child: const Text('Otvori'),
        ),
      ),
    );
  }
}

class _Result {
  String? value;
}

Future<_Result> _openDialog(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final result = _Result();
  await tester.pumpWidget(
    MaterialApp(home: _DialogHost(onResult: (value) => result.value = value)),
  );
  await tester.tap(find.text('Otvori'));
  await tester.pumpAndSettle();
  return result;
}

void main() {
  testWidgets('does not show an error when the dialog opens', (
    WidgetTester tester,
  ) async {
    await _openDialog(tester);

    expect(find.text('Razlog je obavezan.'), findsNothing);
    final confirmButton = tester.widget<ElevatedButton>(
      find.widgetWithText(ElevatedButton, 'Potvrdi'),
    );
    expect(confirmButton.onPressed, isNull);
  });

  testWidgets('shows Razlog je obavezan. after the user types and clears the '
      'text', (WidgetTester tester) async {
    await _openDialog(tester);

    expect(find.text('Razlog je obavezan.'), findsNothing);

    await tester.enterText(find.byType(TextField), 'Popunjeno');
    await tester.pump();
    expect(find.text('Razlog je obavezan.'), findsNothing);

    await tester.enterText(find.byType(TextField), '');
    await tester.pump();
    expect(find.text('Razlog je obavezan.'), findsOneWidget);
  });

  testWidgets('Potvrdi is enabled for a valid reason and returns the trimmed '
      'text', (WidgetTester tester) async {
    final result = await _openDialog(tester);

    expect(find.text('Razlog je obavezan.'), findsNothing);

    await tester.enterText(find.byType(TextField), '  Popunjeno  ');
    await tester.pump();

    final confirmButton = tester.widget<ElevatedButton>(
      find.widgetWithText(ElevatedButton, 'Potvrdi'),
    );
    expect(confirmButton.onPressed, isNotNull);

    await tester.tap(find.widgetWithText(ElevatedButton, 'Potvrdi'));
    await tester.pumpAndSettle();

    expect(result.value, 'Popunjeno');
  });
}
