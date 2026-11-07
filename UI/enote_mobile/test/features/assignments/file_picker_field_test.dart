import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_mobile/features/assignments/file_picker_field.dart';

class _NullBytesPicker extends FilePicker {
  @override
  Future<FilePickerResult?> pickFiles({
    String? dialogTitle,
    String? initialDirectory,
    FileType type = FileType.any,
    List<String>? allowedExtensions,
    Function(FilePickerStatus)? onFileLoading,
    bool allowCompression = false,
    int compressionQuality = 0,
    bool allowMultiple = false,
    bool withData = false,
    bool withReadStream = false,
    bool lockParentWindow = false,
    bool readSequential = false,
  }) async {
    return FilePickerResult([PlatformFile(name: 'zadaca.pdf', size: 10)]);
  }
}

void main() {
  test('assignmentFileLabel shows no raw file name', () {
    expect(
      assignmentFileLabel('/api/v1/uploads/assignments/abc.pdf'),
      'PDF datoteka',
    );
    expect(assignmentFileLabel('/api/v1/uploads/assignments/abc.PNG'), 'Slika');
    expect(assignmentFileLabel('/api/v1/uploads/assignments/abc.jpg'), 'Slika');
    expect(assignmentFileLabel(null), 'Nema datoteke');
  });

  test('isAssignmentImage is true only for images', () {
    expect(isAssignmentImage('/x/abc.jpeg'), isTrue);
    expect(isAssignmentImage('/x/abc.pdf'), isFalse);
    expect(isAssignmentImage(null), isFalse);
  });

  test('validateAssignmentFile keeps the 5 MiB limit at its boundary', () {
    expect(validateAssignmentFile('a.pdf', 5242880), isNull);
    expect(validateAssignmentFile('a.pdf', 5242881), isNotNull);
  });

  FilePicker? realPicker;
  try {
    realPicker = FilePicker.platform;
  } catch (_) {}

  tearDown(() {
    final real = realPicker;
    if (real != null) FilePicker.platform = real;
  });

  testWidgets('a file with null bytes shows the unreadable-file error', (
    tester,
  ) async {
    FilePicker.platform = _NullBytesPicker();

    Object? picked = 'sentinel';
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: FilePickerField(onChanged: (file) => picked = file),
        ),
      ),
    );

    await tester.tap(find.text('Odaberi datoteku'));
    await tester.pumpAndSettle();

    expect(
      find.text('Datoteku nije moguće pročitati. Pokušajte ponovo.'),
      findsOneWidget,
    );
    expect(picked, 'sentinel');
  });
}
