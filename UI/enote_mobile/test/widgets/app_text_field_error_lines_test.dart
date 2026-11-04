import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/widgets/app_text_field.dart';

const _message =
    'Unesite broj telefona sa 6 do 15 cifara, opcionalno sa + na početku; '
    'razmaci su dozvoljeni (npr. +387 61 123 456).';

void main() {
  testWidgets('shows the full phone validator message on more than one line', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(375, 812);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final controller = TextEditingController(text: 'abc');
    addTearDown(controller.dispose);
    final formKey = GlobalKey<FormState>();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Form(
            key: formKey,
            child: AppTextField(
              controller: controller,
              label: 'Broj telefona',
              validator: Validators.optionalPhone,
            ),
          ),
        ),
      ),
    );

    formKey.currentState!.validate();
    await tester.pumpAndSettle();

    expect(find.text(_message), findsOneWidget);

    final field = tester.widget<TextField>(find.byType(TextField));
    expect(field.decoration!.errorMaxLines, greaterThan(1));
  });
}
