import 'package:enote_core/enote_core.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

Future<Uint8List?> pickImageBytes() async {
  final result = await FilePicker.platform.pickFiles(
    type: FileType.image,
    withData: true,
  );
  if (result == null || result.files.isEmpty) return null;
  return result.files.first.bytes;
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
}) =>
    uploadImageWith(
      () => provider
          .uploadImage(id, bytes, fileName, contentType)
          .then(onSuccess),
      context: context,
    );
