import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/profile/edit_profile_dialog.dart';
import 'package:enote_desktop/features/profile/profile_provider.dart';
import 'package:enote_desktop/widgets/entity_form_scaffold.dart';

import '../../helpers.dart';

const _profile = {
  'role': 'Instructor',
  'username': 'instructor',
  'email': 'instructor@enote.com',
  'profile': {r'$type': 'instructor', 'firstName': 'Selma', 'lastName': 'Begić'},
};

ApiClient _apiClient() {
  final client = ScriptedClient((_) => jsonResponse(_profile, 200));
  return ApiClient(
    baseUrl: 'http://test/api/v1/',
    authState: AuthState(tokenReader: () => fakeJwt(), httpClient: client),
    httpClient: client,
  );
}

void main() {
  test('each change of the picture gives a new picture address', () async {
    final provider = ProfileProvider(apiClient: _apiClient());
    final seen = {provider.pictureUrl()};

    await provider.uploadPicture([1, 2, 3], 'a.png', 'image/png');
    expect(seen.add(provider.pictureUrl()), isTrue);

    await provider.deletePicture();
    expect(seen.add(provider.pictureUrl()), isTrue);

    await provider.getProfile();
    expect(seen.add(provider.pictureUrl()), isTrue);
  });

  testWidgets('a reopened edit form does not use the address of the old picture', (
    tester,
  ) async {
    final apiClient = _apiClient();
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiClient>.value(value: apiClient),
          Provider<ProfileProvider>(
            create: (_) => ProfileProvider(apiClient: apiClient),
          ),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => FilledButton(
                onPressed: () => EntityFormScaffold.showAsDialog(
                  context,
                  builder: (_) => const EditProfileDialog(
                    initialFirstName: 'Selma',
                    initialLastName: 'Begić',
                    initialEmail: 'instructor@enote.com',
                    initialDateOfBirth: null,
                    initialHasPicture: false,
                  ),
                ),
                child: const Text('otvori'),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('otvori'));
    await tester.pumpAndSettle();
    final field = tester.widget<ImageField>(find.byType(ImageField));
    final oldAddress = field.imageUrl;

    final uploaded = field.onUpload!(
      Uint8List.fromList([1, 2, 3]),
      'a.png',
      'image/png',
    );
    await tester.pumpAndSettle();
    final newAddress = await uploaded;
    expect(newAddress, isNot(oldAddress));

    await tester.tap(find.text('Otkaži'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('otvori'));
    await tester.pumpAndSettle();

    final reopened = tester.widget<ImageField>(find.byType(ImageField));
    expect(reopened.imageUrl, newAddress);
  });
}
