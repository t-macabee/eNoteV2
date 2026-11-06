import 'dart:typed_data';

const int maxUploadBytes = 5 * 1024 * 1024;
const List<String> allowedImageExtensions = ['jpg', 'jpeg', 'png', 'webp'];
const String imageRulesMessage = 'Dozvoljeni formati: JPG, PNG, WebP (do 5 MB).';

class PickedImage {
  final Uint8List bytes;
  final String fileName;
  final String contentType;

  const PickedImage({
    required this.bytes,
    required this.fileName,
    required this.contentType,
  });
}

String fileExtension(String fileName) {
  final dot = fileName.lastIndexOf('.');
  return dot == -1 ? '' : fileName.substring(dot + 1).toLowerCase();
}

PickedImage? pickedImageOrNull(Uint8List bytes, String fileName) {
  final contentType = switch (fileExtension(fileName)) {
    'jpg' || 'jpeg' => 'image/jpeg',
    'png' => 'image/png',
    'webp' => 'image/webp',
    _ => null,
  };
  if (contentType == null || bytes.length > maxUploadBytes) return null;
  return PickedImage(bytes: bytes, fileName: fileName, contentType: contentType);
}
