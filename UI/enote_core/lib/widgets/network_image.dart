import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../theme/app_theme.dart';
import '../theme/app_tokens.dart';

Widget networkImageOrPlaceholder(
  String? url,
  ApiClient? apiClient, {
  required double size,
  required double borderRadius,
  required Widget Function() placeholder,
}) {
  if (url == null || url.trim().isEmpty) {
    return placeholder();
  }
  final trimmed = url.trim();
  if (trimmed.startsWith('/')) {
    final client = apiClient;
    if (client == null) return placeholder();
    final uri = Uri.tryParse(client.baseUrl);
    final origin = (uri != null && uri.hasScheme)
        ? uri.origin
        : client.baseUrl.replaceAll(RegExp(r'/api/v\d+/?$'), '');
    return _networkImage(
      '$origin$trimmed',
      headers: client.authHeaders,
      size: size,
      borderRadius: borderRadius,
      placeholder: placeholder,
    );
  }
  return placeholder();
}

Widget _networkImage(
  String url, {
  Map<String, String>? headers,
  required double size,
  required double borderRadius,
  required Widget Function() placeholder,
}) {
  return ClipRRect(
    borderRadius: BorderRadius.circular(borderRadius),
    child: Image.network(
      url,
      headers: headers,
      width: size,
      height: size,
      fit: BoxFit.cover,
      errorBuilder: (_, _, _) => placeholder(),
    ),
  );
}

class ImagePlaceholder extends StatelessWidget {
  final double width;
  final double height;
  final double borderRadius;
  final double? iconSize;

  const ImagePlaceholder({
    super.key,
    required this.width,
    required this.height,
    this.borderRadius = 0,
    this.iconSize,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: width,
      height: height,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: AppTheme.surfaceContainer,
        borderRadius: BorderRadius.circular(borderRadius),
      ),
      child: Icon(
        Icons.image_outlined,
        size: iconSize ?? AppSize.placeholderIcon,
        color: AppTheme.textTertiary,
      ),
    );
  }
}
