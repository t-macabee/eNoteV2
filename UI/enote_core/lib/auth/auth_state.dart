import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:jwt_decoder/jwt_decoder.dart';

import '../api/api_error_mapper.dart';
import '../models/identity/auth_models.dart';

class AuthState extends ChangeNotifier {
  String _baseUrl;
  final String? Function()? _tokenReader;
  final void Function(String? token)? _tokenWriter;

  String? _accessToken;
  int? _userId;
  String? _username;
  List<String> _roles = [];

  AuthState({
    this._baseUrl = '',
    this._tokenReader,
    this._tokenWriter,
  }) {
    _accessToken = _tokenReader?.call();
    _decodeToken(_accessToken);
  }

  String? get accessToken => _accessToken;
  int? get userId => _userId;
  String? get username => _username;
  List<String> get roles => List.unmodifiable(_roles);
  bool get isAuthenticated => _accessToken != null && !_isTokenExpired;

  bool get _isTokenExpired {
    if (_accessToken == null) return true;
    try {
      return JwtDecoder.isExpired(_accessToken!);
    } catch (_) {
      return true;
    }
  }

  bool hasRole(String role) => _roles.contains(role);

  String? get topRole {
    const order = ['Administrator', 'Instructor', 'StoreEmployee', 'Student'];
    for (final role in order) {
      if (_roles.contains(role)) {
        return role;
      }
    }
    return _roles.isNotEmpty ? _roles.first : null;
  }

  void _decodeToken(String? token) {
    if (token == null) {
      _userId = null;
      _username = null;
      _roles = [];
      return;
    }
    try {
      final decoded = JwtDecoder.decode(token);
      _userId = _parseInt(decoded['sub']);
      _username = decoded['unique_name'] as String?;
      final roleClaim = decoded['role'];
      final roleUri =
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
      if (roleClaim is List<dynamic>) {
        _roles = roleClaim.map((e) => e.toString()).toList();
      } else if (roleClaim is String) {
        _roles = [roleClaim];
      } else if (decoded.containsKey(roleUri)) {
        final uriClaim = decoded[roleUri];
        if (uriClaim is List<dynamic>) {
          _roles = uriClaim.map((e) => e.toString()).toList();
        } else if (uriClaim is String) {
          _roles = [uriClaim];
        }
      }
    } catch (_) {
      _accessToken = null;
      _userId = null;
      _username = null;
      _roles = [];
    }
  }

  int? _parseInt(dynamic value) {
    if (value == null) return null;
    if (value is int) return value;
    if (value is String) return int.tryParse(value);
    return null;
  }

  Future<void> login(String username, String password) async {
    final uri = Uri.parse('${_baseUrl}auth/login');
    final body = jsonEncode({'username': username, 'password': password});

    final response = await http.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: body,
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
          ApiErrorMapper.mapError(response.statusCode, response.body));
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final authResponse = AuthResponse.fromJson(data);

    _accessToken = authResponse.token;
    _tokenWriter?.call(_accessToken);
    _decodeToken(_accessToken);
    notifyListeners();
  }

  Future<void> logout() {
    _accessToken = null;
    _userId = null;
    _username = null;
    _roles = [];
    _tokenWriter?.call(null);
    notifyListeners();
    return Future.value();
  }

  void setToken(String token) {
    _accessToken = token;
    _tokenWriter?.call(token);
    _decodeToken(token);
  }

  set baseUrl(String value) {
    _baseUrl = value;
  }
}
