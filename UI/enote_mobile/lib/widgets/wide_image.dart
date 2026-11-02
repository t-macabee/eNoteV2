import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

Widget wideNetworkImage(
  String? url,
  ApiClient? apiClient, {
  required double width,
  required double height,
  double borderRadius = AppRadius.md,
}) {
  return ClipRRect(
    borderRadius: BorderRadius.circular(borderRadius),
    child: ClipRect(
      child: Align(
        alignment: Alignment.center,
        heightFactor: height / width,
        child: networkImageOrPlaceholder(
          url,
          apiClient,
          size: width,
          borderRadius: 0,
          placeholder: () => ImagePlaceholder(width: width, height: width),
        ),
      ),
    ),
  );
}

class HeroImage extends StatelessWidget {
  final String? imageUrl;
  final ApiClient? apiClient;

  const HeroImage({super.key, this.imageUrl, this.apiClient});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) => wideNetworkImage(
        imageUrl,
        apiClient,
        width: constraints.maxWidth,
        height: constraints.maxWidth * 9 / 16,
        borderRadius: AppRadius.lg,
      ),
    );
  }
}
