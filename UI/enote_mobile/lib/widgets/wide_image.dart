import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../theme/app_theme.dart';

/// A wide (non-square) image box built on core [networkImageOrPlaceholder].
///
/// The core helper always renders a square of `size`, which is right for the
/// 56/72 px list thumbnails but not for the 16:9 hero (S8) or the 160×88
/// recommendation card (S7). Both keep using it: the square is laid out at
/// the target *width* and then clipped to the target height around its
/// centre, so the placeholder branch and the auth-header/relative-path rules
/// stay exactly the ones core defines.
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

/// Full-width 16:9 hero image (S8).
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
