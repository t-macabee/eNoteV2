import 'package:http/http.dart' as http;

class SessionHttpClient extends http.BaseClient {
  final http.Client _inner;
  final void Function()? onUnauthorized;
  final Duration timeout;

  String? _lastNotifiedToken;
  bool _notifiedNull = false;

  SessionHttpClient({
    http.Client? inner,
    this.onUnauthorized,
    this.timeout = const Duration(seconds: 20),
  }) : _inner = inner ?? http.Client();

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final response = await _inner.send(request).timeout(timeout);
    if (response.statusCode == 401 && !_isAuthPath(request.url)) {
      final token = _bearerToken(request);
      if (token == null) {
        if (!_notifiedNull) {
          _notifiedNull = true;
          onUnauthorized?.call();
        }
      } else if (token != _lastNotifiedToken) {
        _lastNotifiedToken = token;
        onUnauthorized?.call();
      }
    }
    return response;
  }

  bool _isAuthPath(Uri url) => url.path.contains('/auth/');

  String? _bearerToken(http.BaseRequest request) {
    final header =
        request.headers['Authorization'] ?? request.headers['authorization'];
    if (header == null) {
      return null;
    }
    const prefix = 'Bearer ';
    return header.startsWith(prefix) ? header.substring(prefix.length) : header;
  }
}
