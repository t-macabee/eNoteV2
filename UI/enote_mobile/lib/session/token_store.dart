import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class TokenStore {
  static const String tokenKey = 'enote.access_token';

  final FlutterSecureStorage _storage;
  String? _cached;

  TokenStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  Future<void> load() async {
    try {
      _cached = await _storage.read(key: tokenKey);
    } catch (e) {
      debugPrint('TokenStore load failed: $e');
    }
  }

  String? read() => _cached;

  void write(String? token) {
    _cached = token;
    unawaited(_persist(token));
  }

  Future<void> _persist(String? token) async {
    try {
      if (token == null) {
        await _storage.delete(key: tokenKey);
      } else {
        await _storage.write(key: tokenKey, value: token);
      }
    } catch (e) {
      debugPrint('TokenStore persist failed: $e');
    }
  }
}
