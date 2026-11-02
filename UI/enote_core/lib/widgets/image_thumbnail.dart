import 'package:flutter/material.dart';

import '../api/api_client.dart';
import 'network_image.dart';

class ImageThumbnail extends StatelessWidget {
  final String? imageUrl;
  final double size;
  final double borderRadius;

  final ApiClient? apiClient;

  const ImageThumbnail({
    super.key,
    this.imageUrl,
    this.size = 40,
    this.borderRadius = 4,
    this.apiClient,
  });

  @override
  Widget build(BuildContext context) {
    return networkImageOrPlaceholder(
      imageUrl,
      apiClient,
      size: size,
      borderRadius: borderRadius,
      placeholder: _placeholder,
    );
  }

  Widget _placeholder() {
    return ImagePlaceholder(
      width: size,
      height: size,
      borderRadius: borderRadius,
      iconSize: size * 0.5,
    );
  }
}
