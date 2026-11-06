import 'package:enote_core/enote_core.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

Future<PickedImage?> pickImage(BuildContext context) async {
  final result = await FilePicker.platform.pickFiles(
    type: FileType.custom,
    allowedExtensions: allowedImageExtensions,
    withData: true,
  );
  if (result == null || result.files.isEmpty) return null;
  final file = result.files.first;
  final bytes = file.bytes;
  final picked = bytes == null ? null : pickedImageOrNull(bytes, file.name);
  if (picked == null && context.mounted) {
    ErrorBanner.show(context, message: imageRulesMessage);
  }
  return picked;
}

Future<String?> uploadImageWith(
  Future<String?> Function() upload, {
  required BuildContext context,
}) async {
  try {
    final imagePath = await upload();
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Slika uspješno postavljena.')),
      );
    }
    return imagePath;
  } catch (e) {
    if (context.mounted) {
      ErrorBanner.show(context, message: userMessage(e));
    }
    return null;
  }
}

Future<String?> uploadImageFor<T>(
  CrudProvider<T> provider,
  int id,
  Uint8List bytes,
  String fileName,
  String contentType, {
  required BuildContext context,
  required String? Function(T updated) onSuccess,
}) => uploadImageWith(
  () => provider.uploadImage(id, bytes, fileName, contentType).then(onSuccess),
  context: context,
);
