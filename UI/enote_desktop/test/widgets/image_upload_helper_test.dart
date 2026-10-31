import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/image_upload_helper.dart';

class _FakeCrud extends CrudProvider<InstrumentDto> {
  int uploadCalls = 0;

  _FakeCrud()
      : super(
          endpoint: 'shop/instruments',
          apiClient: ApiClient(
            baseUrl: 'http://localhost/',
            authState: AuthState(baseUrl: 'http://localhost/'),
          ),
        );

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);

  @override
  Future<InstrumentDto> uploadImage(
    int id,
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    uploadCalls++;
    return InstrumentDto.fromJson({
      'id': id,
      'model': 'M',
      'manufacturer': 'M',
      'instrumentTypeId': 1,
      'instrumentType': 'Gitara',
      'musicStore': 'Shop',
      'isAvailable': true,
      'imagePath': '/images/$id.jpg',
    });
  }
}

Future<BuildContext> _pumpHarness(WidgetTester tester) async {
  tester.view.physicalSize = const Size(800, 600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  late BuildContext captured;
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) {
            captured = context;
            return const SizedBox();
          },
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return captured;
}

void main() {
  testWidgets('F4-06: uploadImageWith shows snackbar and returns path',
      (tester) async {
    final context = await _pumpHarness(tester);

    final result = await uploadImageWith(
      () async => '/images/store.jpg',
      context: context,
    );
    await tester.pump();

    expect(result, equals('/images/store.jpg'));
    expect(
      find.text('Slika uspješno postavljena.'),
      findsOneWidget,
    );
  });

  testWidgets('F4-06: uploadImageWith shows banner and returns null',
      (tester) async {
    final context = await _pumpHarness(tester);

    final result = await uploadImageWith(
      () async => throw ApiException('Greška.'),
      context: context,
    );
    await tester.pump();

    expect(result, isNull);
    expect(find.text('Greška.'), findsOneWidget);
  });

  testWidgets('F4-06: uploadImageFor delegates to the shared flow',
      (tester) async {
    final context = await _pumpHarness(tester);
    final provider = _FakeCrud();

    final result = await uploadImageFor<InstrumentDto>(
      provider,
      7,
      Uint8List(0),
      'img.jpg',
      'image/jpeg',
      context: context,
      onSuccess: (updated) => updated.imagePath,
    );
    await tester.pump();

    expect(provider.uploadCalls, equals(1));
    expect(result, equals('/images/7.jpg'));
    expect(
      find.text('Slika uspješno postavljena.'),
      findsOneWidget,
    );
  });
}
