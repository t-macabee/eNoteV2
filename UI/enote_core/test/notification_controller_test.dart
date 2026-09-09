import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';

Map<String, dynamic> _item(int id) => {
      'id': id,
      'title': 't$id',
      'body': 'b$id',
      'isRead': false,
      'createdAt': '2026-09-01T10:00:00Z',
    };

class _PagedFakeClient extends http.BaseClient {
  final Map<int, List<Map<String, dynamic>>> pages;
  final List<Uri> urls = [];

  _PagedFakeClient(this.pages);

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    urls.add(request.url);
    final page =
        int.tryParse(request.url.queryParameters['page'] ?? '1') ?? 1;
    final body = jsonEncode({'items': pages[page] ?? []});
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

NotificationController _controller(_PagedFakeClient client) {
  final auth = AuthState();
  final api =
      ApiClient(baseUrl: 'http://localhost:5059/api/v1/', authState: auth, httpClient: client);
  return NotificationController(apiClient: api, endpoint: 'student/notifications');
}

void main() {
  test('refresh sets hasMore true iff a full page', () async {
    final client = _PagedFakeClient({
      1: [_item(1), _item(2)],
    });
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));
    expect(c.notifications.length, 2);
    expect(c.hasMore, isTrue);

    final client2 = _PagedFakeClient({
      1: [_item(1)],
    });
    final c2 = _controller(client2);
    await c2.refresh(search: NotificationSearchObject(pageSize: 2));
    expect(c2.hasMore, isFalse);
  });

  test('loadMore requests page 2, appends, hasMore false on short page',
      () async {
    final client = _PagedFakeClient({
      1: [_item(1), _item(2)],
      2: [_item(3)],
    });
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));
    await c.loadMore();
    expect(c.notifications.length, 3);
    expect(c.hasMore, isFalse);
    expect(client.urls.last.queryParameters['page'], '2');
  });

  test('refresh after loadMore resets to page 1', () async {
    final client = _PagedFakeClient({
      1: [_item(1), _item(2)],
      2: [_item(3), _item(4)],
    });
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));
    await c.loadMore();
    expect(c.notifications.length, 4);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));
    expect(c.notifications.length, 2);
    expect(client.urls.last.queryParameters['page'], '1');
  });
}
