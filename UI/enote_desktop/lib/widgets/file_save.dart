import 'dart:typed_data';

import 'package:enote_core/enote_core.dart';
import 'package:file_picker/file_picker.dart';

typedef FileSaver = Future<String?> Function(Uint8List bytes, String fileName);

Future<String?> saveFileWithDialog(Uint8List bytes, String fileName) async {
  try {
    final uri = await FilePicker.saveFile(
      dialogTitle: 'Sačuvaj datoteku',
      fileName: fileName,
      bytes: bytes,
    );
    return uri?.toFilePath();
  } on Exception {
    throw ApiException(
      'Datoteku nije moguće sačuvati. Provjerite odabranu lokaciju.',
    );
  }
}
