import 'package:flutter/material.dart';

import '../api/api_client.dart';

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
  if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
    return _networkImage(
      trimmed,
      size: size,
      borderRadius: borderRadius,
      placeholder: placeholder,
    );
  }
  if (trimmed.startsWith('/')) {
    final client = apiClient;
    if (client == null) return placeholder();
    return _networkImage(
      '${client.baseUrl}$trimmed',
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
