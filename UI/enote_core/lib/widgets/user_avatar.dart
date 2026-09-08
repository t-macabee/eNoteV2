import 'package:flutter/material.dart';

import '../api/api_client.dart';
import 'network_image.dart';

/// Origin-relative URL for `GET users/{userId}/picture` (self-service
/// `users/me/picture` when [userId] is `'me'`).
///
/// The leading `/` matters: [networkImageOrPlaceholder] resolves it against
/// the client's origin and attaches the bearer headers. A 404 (no picture or
/// not visible) is the normal case — callers must fall back to initials or a
/// role icon via the placeholder, never an error banner.
String userPictureUrl(ApiClient client, Object userId, {int? cacheBuster}) {
  final baseUri = Uri.tryParse(client.baseUrl);
  var basePath = baseUri?.path ?? '/api/v1/';
  if (!basePath.endsWith('/')) basePath = '$basePath/';
  var path = '${basePath}users/$userId/picture';
  if (cacheBuster != null) path = '$path?v=$cacheBuster';
  return path;
}

/// Circular user avatar: the picture at [userPictureUrl], or [placeholder]
/// (typically initials) when there is none. A 404 from the picture endpoint
/// renders the placeholder via [errorBuilder], never an error.
class UserAvatar extends StatelessWidget {
  final Object userId;
  final ApiClient apiClient;
  final double radius;
  final Widget Function() placeholder;

  const UserAvatar({
    super.key,
    required this.userId,
    required this.apiClient,
    required this.placeholder,
    this.radius = 28,
  });

  @override
  Widget build(BuildContext context) {
    return networkImageOrPlaceholder(
      userPictureUrl(apiClient, userId),
      apiClient,
      size: radius * 2,
      borderRadius: radius,
      placeholder: placeholder,
    );
  }
}
