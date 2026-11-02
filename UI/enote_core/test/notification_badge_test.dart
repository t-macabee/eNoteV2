import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_core/theme/app_theme.dart';

class _UnreadCountClient extends http.BaseClient {
  final int unreadCount;

  _UnreadCountClient(this.unreadCount);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode({'unreadCount': unreadCount}))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

Future<NotificationController> _controllerWithUnread(int count) async {
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: _UnreadCountClient(count),
  );
  final controller = NotificationController(
    apiClient: apiClient,
    endpoint: 'notifications',
  );
  await controller.refreshUnreadCount();
  return controller;
}

Future<void> _pump(WidgetTester tester, NotificationController controller) {
  return tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.dark,
      home: Scaffold(
        body: NotificationBadge(controller: controller, onTap: () {}),
      ),
    ),
  );
}

void main() {
  testWidgets('NotificationBadge shows the count in a Badge', (tester) async {
    await _pump(tester, await _controllerWithUnread(3));

    final badge = tester.widget<Badge>(find.byType(Badge));
    expect(badge.isLabelVisible, isTrue);
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets('NotificationBadge hides the label at zero', (tester) async {
    await _pump(tester, await _controllerWithUnread(0));

    final badge = tester.widget<Badge>(find.byType(Badge));
    expect(badge.isLabelVisible, isFalse);
  });

  testWidgets('NotificationBadge caps the count at 99+', (tester) async {
    await _pump(tester, await _controllerWithUnread(120));

    expect(find.text('99+'), findsOneWidget);
  });
}
