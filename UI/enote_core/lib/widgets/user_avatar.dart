import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../api/api_response.dart';
import 'network_image.dart';

Future<void> uploadUserPicture(
  ApiClient client,
  List<int> bytes,
  String fileName,
  String contentType,
) async {
  final response = await client.putMultipart(
    'users/me/picture',
    bytes: bytes,
    fileName: fileName,
    contentType: contentType,
  );
  throwIfError(response);
}

String userPictureUrl(ApiClient client, Object userId, {int? cacheBuster}) {
  final baseUri = Uri.tryParse(client.baseUrl);
  var basePath = baseUri?.path ?? '/api/v1/';
  if (!basePath.endsWith('/')) basePath = '$basePath/';
  var path = '${basePath}users/$userId/picture';
  if (cacheBuster != null) path = '$path?v=$cacheBuster';
  return path;
}

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
