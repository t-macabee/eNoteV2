import 'dart:io';
import 'dart:typed_data';

import 'package:enote_core/enote_core.dart';
import 'package:file_picker/file_picker.dart';

typedef FileSaver = Future<String?> Function(Uint8List bytes, String fileName);

Future<String?> saveFileWithDialog(Uint8List bytes, String fileName) async {
  try {
    final path = await FilePicker.platform.saveFile(
      dialogTitle: 'Sačuvaj datoteku',
      fileName: fileName,
      type: FileType.custom,
      allowedExtensions: [fileExtension(fileName)],
    );
    if (path == null) return null;
    await File(path).writeAsBytes(bytes);
    return path;
  } on Exception {
    throw ApiException(
      'Datoteku nije moguće sačuvati. Provjerite odabranu lokaciju.',
    );
  }
}
