import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

Widget wideNetworkImage(
  String? url,
  ApiClient? apiClient, {
  required double width,
  required double height,
  double borderRadius = 8,
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
          placeholder: () => _placeholder(width),
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
        borderRadius: 12,
      ),
    );
  }
}

Widget _placeholder(double size) {
  return Container(
    width: size,
    height: size,
    color: AppTheme.surfaceContainer,
    child: Icon(
      Icons.image_outlined,
      size: 32,
      color: AppTheme.textTertiary,
    ),
  );
}
