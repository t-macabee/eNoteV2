import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/widgets/file_save.dart';

import '../helpers.dart';

const _failedMessage =
    'Datoteku nije moguće sačuvati. Provjerite odabranu lokaciju.';

class _FailingPicker extends FakeSaveFilePicker {
  @override
  Future<String?> saveFile({
    String? dialogTitle,
    String? fileName,
    String? initialDirectory,
    FileType type = FileType.any,
    List<String>? allowedExtensions,
    Uint8List? bytes,
    bool lockParentWindow = false,
  }) async => throw Exception('No file dialog is available.');
}

void main() {
  late Directory folder;
  final bytes = Uint8List.fromList([37, 80, 68, 70]);

  setUp(() => folder = Directory.systemTemp.createTempSync('enote_file_save'));
  tearDown(() => folder.deleteSync(recursive: true));

  void expectFailedMessage(Object error) {
    expect(error, isA<ApiException>());
    expect(userMessage(error), _failedMessage);
  }

  test('a location that cannot be written gives the save message', () async {
    FilePicker.platform = FakeSaveFilePicker(path: '${folder.path}/nema/a.pdf');

    await expectLater(
      saveFileWithDialog(bytes, 'a.pdf'),
      throwsA(
        predicate((Object e) {
          expectFailedMessage(e);
          return true;
        }),
      ),
    );
  });

  test('a failed file dialog gives the save message', () async {
    FilePicker.platform = _FailingPicker();

    await expectLater(
      saveFileWithDialog(bytes, 'a.pdf'),
      throwsA(
        predicate((Object e) {
          expectFailedMessage(e);
          return true;
        }),
      ),
    );
  });
}
