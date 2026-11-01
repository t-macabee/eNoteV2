import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/widgets/entity_form_sections.dart';
import 'package:enote_desktop/widgets/user_credential_fields.dart';

void main() {
  UserCredentialFieldsController? controller;

  tearDown(() {
    controller?.dispose();
    controller = null;
  });

  testWidgets('renders two sections with all five fields', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    controller = UserCredentialFieldsController();
    final formKey = GlobalKey<FormState>();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Form(
            key: formKey,
            child: UserCredentialFields(controller: controller!),
          ),
        ),
      ),
    );

    expect(find.byType(FormSection), findsNWidgets(2));
    expect(find.text('Pristupni podaci'), findsOneWidget);
    expect(find.text('Lični podaci'), findsOneWidget);
    for (final label in [
      'Korisničko ime',
      'Email',
      'Lozinka',
      'Ime',
      'Prezime',
    ]) {
      expect(find.text(label), findsOneWidget);
    }
  });

  testWidgets('validators still run', (tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    controller = UserCredentialFieldsController();
    final formKey = GlobalKey<FormState>();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Form(
            key: formKey,
            child: UserCredentialFields(controller: controller!),
          ),
        ),
      ),
    );

    expect(formKey.currentState!.validate(), isFalse);
    await tester.pump();

    expect(find.text('Korisničko ime je obavezan.'), findsOneWidget);
    expect(find.text('Email adresa je obavezna.'), findsOneWidget);
    expect(find.text('Lozinka je obavezna.'), findsOneWidget);
  });

  testWidgets('username and email share one row on wide surface', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    controller = UserCredentialFieldsController();
    final formKey = GlobalKey<FormState>();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Form(
            key: formKey,
            child: UserCredentialFields(controller: controller!),
          ),
        ),
      ),
    );

    final usernameField = find.widgetWithText(TextFormField, 'Korisničko ime');
    final emailField = find.widgetWithText(TextFormField, 'Email');

    expect(
      tester.getTopLeft(usernameField).dx,
      lessThan(tester.getTopLeft(emailField).dx),
    );
    expect(
      tester.getTopLeft(usernameField).dy,
      tester.getTopLeft(emailField).dy,
    );
  });
}
