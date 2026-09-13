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
  int unreadCount;
  final bool throwOnUnreadCount;
  final Set<int> failPages;

  _PagedFakeClient(
    this.pages, {
    this.unreadCount = 0,
    this.throwOnUnreadCount = false,
    this.failPages = const {},
  });

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    urls.add(request.url);
    if (request.url.path.endsWith('/unread-count')) {
      if (throwOnUnreadCount) {
        throw http.ClientException('offline');
      }
      final body = jsonEncode({'unreadCount': unreadCount});
      return http.StreamedResponse(
        Stream.value(utf8.encode(body)),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    if (request.method == 'PATCH') {
      return http.StreamedResponse(
        Stream.value(utf8.encode('{"message":"OK"}')),
        200,
        headers: {'content-type': 'application/json'},
      );
    }
    final page =
        int.tryParse(request.url.queryParameters['page'] ?? '1') ?? 1;
    if (failPages.contains(page)) {
      return http.StreamedResponse(
        Stream.value(utf8.encode('{"message":"Server error"}')),
        500,
        headers: {'content-type': 'application/json'},
      );
    }
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

int _listRequests(_PagedFakeClient client, int page) => client.urls
    .where(
      (u) =>
          !u.path.endsWith('/unread-count') &&
          u.queryParameters['page'] == '$page',
    )
    .length;

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

  test(
      'page 1 has 2 unread, server count is 7, markRead on one becomes 6, markAllRead becomes 0',
      () async {
    final client = _PagedFakeClient(
      {
        1: [_item(1), _item(2)],
      },
      unreadCount: 7,
    );
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));
    expect(c.notifications.where((n) => !n.isRead).length, 2);
    expect(c.unreadCount, 7);

    await c.markRead(1);
    expect(c.unreadCount, 6);

    await c.markAllRead();
    expect(c.unreadCount, 0);
  });

  test('refresh succeeds even when unread-count throws', () async {
    final client = _PagedFakeClient(
      {
        1: [_item(1)],
      },
      throwOnUnreadCount: true,
    );
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));
    expect(c.notifications.length, 1);
    expect(c.error, isNull);
  });

  test('concurrent loadMore calls issue a single page 2 request', () async {
    final client = _PagedFakeClient({
      1: [_item(1), _item(2)],
      2: [_item(3), _item(4)],
    });
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));

    await Future.wait([c.loadMore(), c.loadMore()]);

    expect(_listRequests(client, 2), 1);
    expect(c.notifications.length, 4);
  });

  test('loadMore on a failing page 2 keeps the page, surfaces and clears flag',
      () async {
    final client = _PagedFakeClient(
      {
        1: [_item(1), _item(2)],
        2: [_item(3), _item(4)],
      },
      failPages: {2},
    );
    final c = _controller(client);
    await c.refresh(search: NotificationSearchObject(pageSize: 2));

    await expectLater(c.loadMore(), throwsA(isA<ApiException>()));
    expect(c.notifications.length, 2);

    await expectLater(c.loadMore(), throwsA(isA<ApiException>()));
    expect(_listRequests(client, 2), 2);
  });
}
