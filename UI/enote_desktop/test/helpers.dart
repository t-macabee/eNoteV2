import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

String base64UrlSegment(String input) =>
    base64Url.encode(utf8.encode(input)).replaceAll('=', '');

/// Test-only fake JWT. Unified across admin_user_grid, shop_store and other
/// desktop tests (which previously copy-pasted variants with/without
/// isManager). Supports the union of all call sites (T13).
String fakeJwt({
  String subject = '1',
  String username = 'admin',
  String role = 'Administrator',
  bool isManager = false,
}) {
  final header = base64UrlSegment(jsonEncode({'alg': 'none', 'typ': 'JWT'}));
  final payload = base64UrlSegment(jsonEncode({
    'sub': subject,
    'unique_name': username,
    'role': role,
    if (isManager) 'is_manager': true,
    'exp': DateTime.now()
            .add(const Duration(days: 1))
            .millisecondsSinceEpoch ~/
        1000,
  }));
  return '$header.$payload.signature';
}

/// Pumps long enough for [PagedFetchController]'s search debounce to elapse,
/// derived from the actual constant (+100 ms buffer for fake-clock safety) so
/// widget-test pumps and the controller cannot drift (T13).
Future<void> pumpPastDebounce(WidgetTester tester) => tester.pump(
      PagedFetchController.defaultSearchDebounce +
          const Duration(milliseconds: 100),
    );
