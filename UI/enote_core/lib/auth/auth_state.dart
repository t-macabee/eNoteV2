import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:jwt_decoder/jwt_decoder.dart';

import '../api/api_response.dart';
import '../models/identity/auth_models.dart';

class AuthState extends ChangeNotifier {
  final String _baseUrl;
  final String? Function()? _tokenReader;
  final void Function(String? token)? _tokenWriter;
  final http.Client _httpClient;

  String? _accessToken;
  int? _userId;
  String? _username;
  List<String> _roles = [];
  bool _isManager = false;

  AuthState({
    this._baseUrl = '',
    this._tokenReader,
    this._tokenWriter,
    http.Client? httpClient,
  }) : _httpClient = httpClient ?? http.Client() {
    _accessToken = _tokenReader?.call();
    _decodeToken(_accessToken);
  }

  String? get accessToken => _accessToken;
  int? get userId => _userId;
  String? get username => _username;
  List<String> get roles => List.unmodifiable(_roles);
  bool get isManager => _isManager;
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
      _isManager = false;
      return;
    }
    try {
      final decoded = JwtDecoder.decode(token);
      _userId = _parseInt(decoded['sub']);
      _username = decoded['unique_name'] as String?;
      final managerClaim = decoded['is_manager'];
      _isManager = managerClaim == true || managerClaim == 'true';
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
      _isManager = false;
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
    final body =
        jsonEncode(LoginRequest(username: username, password: password).toJson());

    final response = await _httpClient.post(
      uri,
      headers: {'Content-Type': 'application/json'},
      body: body,
    );

    final data = decodeOrThrow(response);
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
    _isManager = false;
    _tokenWriter?.call(null);
    notifyListeners();
    return Future.value();
  }
}
