import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';

import '../auth/auth_state.dart';

class ApiClient {
  final String baseUrl;
  final AuthState authState;
  final http.Client _httpClient;

  ApiClient({
    required this.baseUrl,
    required this.authState,
    http.Client? httpClient,
  }) : _httpClient = httpClient ?? http.Client();

  Map<String, String> get _headers {
    final token = authState.accessToken;
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Uri _uri(String path, [Map<String, dynamic>? queryParams]) {
    final uri = Uri.parse('$baseUrl$path');
    if (queryParams == null || queryParams.isEmpty) {
      return uri;
    }
    return uri.replace(
      queryParameters: queryParams
          .map((k, v) => MapEntry(k, _stringify(v))),
    );
  }

  String? _stringify(dynamic value) {
    if (value == null) return null;
    if (value is bool) return value.toString();
    if (value is DateTime) return value.toIso8601String();
    return value.toString();
  }

  Future<http.Response> get(
    String path, {
    Map<String, dynamic>? queryParams,
  }) async {
    final response = await _httpClient.get(
      _uri(path, queryParams),
      headers: _headers,
    );
    return response;
  }

  Future<http.Response> post(String path, {Object? body}) async {
    final response = await _httpClient.post(
      _uri(path),
      headers: _headers,
      body: body != null ? jsonEncode(body) : null,
    );
    return response;
  }

  Future<http.Response> put(String path, {Object? body}) async {
    final response = await _httpClient.put(
      _uri(path),
      headers: _headers,
      body: body != null ? jsonEncode(body) : null,
    );
    return response;
  }

  Future<http.Response> patch(String path, {Object? body}) async {
    final response = await _httpClient.patch(
      _uri(path),
      headers: _headers,
      body: body != null ? jsonEncode(body) : null,
    );
    return response;
  }

  Future<http.Response> delete(String path) async {
    final response = await _httpClient.delete(
      _uri(path),
      headers: _headers,
    );
    return response;
  }


  Future<http.Response> postMultipart(
    String path, {
    required List<int> bytes,
    required String fileName,
    required String contentType,
  }) async {
    final uri = _uri(path);
    final request = http.MultipartRequest('POST', uri);

    final token = authState.accessToken;
    if (token != null) {
      request.headers['Authorization'] = 'Bearer $token';
    }

    final mediaType = contentType.split('/');
    final multipartFile = http.MultipartFile.fromBytes(
      'file',
      bytes,
      filename: fileName,
      contentType: mediaType.length == 2
          ? MediaType(mediaType[0], mediaType[1])
          : null,
    );
    request.files.add(multipartFile);

    final streamedResponse = await _httpClient.send(request);
    final response = await http.Response.fromStream(streamedResponse);
    return response;
  }


}
