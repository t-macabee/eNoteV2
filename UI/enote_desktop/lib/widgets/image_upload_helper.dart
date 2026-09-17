import 'package:enote_core/enote_core.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

/// Picks an image file and returns its bytes, or null when the user cancels.
Future<Uint8List?> pickImageBytes() async {
  final result = await FilePicker.platform.pickFiles(
    type: FileType.image,
    withData: true,
  );
  if (result == null || result.files.isEmpty) return null;
  return result.files.first.bytes;
}

/// Runs [upload] and applies the form's shared success side effects
/// (snackbar) or error banner.
///
/// Returns whatever [upload] resolves to, or null on failure. [upload] is
/// deliberately id-agnostic so single-record providers (`ShopStoreProvider`,
/// `ProfileProvider`) can share this flow without an id-taking
/// `CrudProvider`.
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

/// Uploads an image for the entity of type [T] held by [provider] and applies
/// the form's success side effects ([onSuccess], snackbar) or error banner.
///
/// Returns the uploaded image path through [onSuccess], or null on failure.
/// [provider] should be the already-resolved provider instance (e.g. from
/// `context.read`), since this helper is deliberately type-agnostic.
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
