import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';

import 'package:enote_mobile/features/assignments/file_picker_field.dart';

base class _UnreadableFile extends PlatformFile {
  @override
  String get name => 'zadaca.pdf';

  @override
  Uri get uri => Uri.parse('file:///zadaca.pdf');

  @override
  get xFile => throw UnimplementedError();

  @override
  int? lengthSync() => null;

  @override
  Future<int?> length() async => null;

  @override
  Future<Uint8List> readAsBytes() async =>
      throw const FileSystemException('unreadable');

  @override
  Stream<Uint8List> readAsByteStream() => throw UnimplementedError();
}

class _UnreadablePicker extends FilePickerPlatform
    with MockPlatformInterfaceMixin {
  @override
  Future<PlatformFile?> pickFile({
    String? dialogTitle,
    String? initialDirectory,
    FileType type = FileType.any,
    List<String>? allowedExtensions,
    Function(FilePickerStatus)? onFileLoading,
    int compressionQuality = 0,
    AndroidOptions androidOptions = const AndroidOptions(),
    DarwinOptions darwinOptions = const DarwinOptions(),
    WindowsOptions windowsOptions = const WindowsOptions(),
    LinuxOptions linuxOptions = const LinuxOptions(),
    WebOptions webOptions = const WebOptions(),
  }) async => _UnreadableFile();
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

  final realPicker = FilePickerPlatform.instance;

  tearDown(() => FilePickerPlatform.instance = realPicker);

  testWidgets('an unreadable file shows the unreadable-file error', (
    tester,
  ) async {
    FilePickerPlatform.instance = _UnreadablePicker();

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
