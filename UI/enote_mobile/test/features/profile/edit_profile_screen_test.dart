import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/profile/edit_profile_screen.dart';
import 'package:enote_mobile/features/profile/profile_provider.dart';
import 'package:enote_mobile/session/session_controller.dart';
import 'package:enote_mobile/theme/app_theme.dart';

import '../../helpers.dart';

Map<String, dynamic> _meJson({bool hasPicture = false}) => {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {
    'id': 7,
    'firstName': 'Student',
    'lastName': 'Enote',
    'dateOfBirth': '2001-05-12T00:00:00',
    'membershipPaidUntil': '2027-09-09T00:00:00',
  },
  'hasPicture': hasPicture,
};

class _ProfileStubClient extends http.BaseClient {
  final List<http.Request> jsonRequests = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    if (request is http.Request) {
      jsonRequests.add(request);
    }
    final body = _bodyFor(request.method, request.url.path);
    return http.StreamedResponse(
      Stream.value(utf8.encode(jsonEncode(body))),
      200,
      headers: {'content-type': 'application/json'},
    );
  }

  Map<String, dynamic> _bodyFor(String method, String path) {
    switch ('$method $path') {
      case 'GET /api/v1/users/me':
        return _meJson();
      case 'GET /api/v1/student/notifications/unread-count':
        return {'unreadCount': 0};
      case 'GET /api/v1/student/notifications':
        return {'items': [], 'totalCount': 0};
      default:
        return {'message': 'OK'};
    }
  }
}

class _Harness {
  final _ProfileStubClient client = _ProfileStubClient();
  late final AuthState authState;
  late final ApiClient apiClient;
  late final SessionController session;

  Future<SessionController> bootstrap() async {
    authState = AuthState(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      tokenReader: () => fakeJwt(),
      tokenWriter: (_) {},
      httpClient: client,
    );
    apiClient = ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    );
    session = SessionController(
      apiClient: apiClient,
      authState: authState,
      notifications: NotificationController(
        apiClient: apiClient,
        endpoint: 'student/notifications',
      ),
    );
    await session.bootstrap();
    return session;
  }

  Widget app(Widget home) {
    return MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider<SessionController>.value(value: session),
        Provider<ProfileProvider>(
          create: (_) => ProfileProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(theme: AppTheme.dark, home: home),
    );
  }
}

Widget _host(Widget screen) {
  return Scaffold(
    body: Builder(
      builder: (context) => Center(
        child: FilledButton(
          onPressed: () => Navigator.of(
            context,
          ).push(MaterialPageRoute(builder: (_) => screen)),
          child: const Text('otvori'),
        ),
      ),
    ),
  );
}

Future<void> _openScreen(WidgetTester tester) async {
  await tester.tap(find.text('otvori'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('edit profile has no obscured password field', (tester) async {
    final harness = _Harness();
    await harness.bootstrap();
    await tester.pumpWidget(harness.app(_host(const EditProfileScreen())));
    await _openScreen(tester);

    final fields = tester.widgetList<TextField>(find.byType(TextField));
    expect(fields, isNotEmpty);
    for (final field in fields) {
      expect(field.obscureText, isFalse, reason: 'no obscureText on S31');
    }
  });

  testWidgets('an invalid email shows the message below the control and '
      'sends no PUT', (tester) async {
    final harness = _Harness();
    await harness.bootstrap();
    await tester.pumpWidget(harness.app(_host(const EditProfileScreen())));
    await _openScreen(tester);

    final emailField = find.byType(TextFormField).at(2);
    await tester.enterText(emailField, 'not-an-email');
    await tester.tap(find.text('Sačuvaj promjene'));
    await tester.pumpAndSettle();

    expect(find.text('Unesite važeću email adresu.'), findsOneWidget);
    final puts = harness.client.jsonRequests
        .where((r) => r.method == 'PUT' && r.url.path == '/api/v1/users/me');
    expect(puts, isEmpty);
  });

  testWidgets('a valid save issues PUT users/me with the UpdateProfileRequest '
      'body shape', (tester) async {
    final harness = _Harness();
    await harness.bootstrap();
    await tester.pumpWidget(harness.app(_host(const EditProfileScreen())));
    await _openScreen(tester);

    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Novi');
    await tester.enterText(fields.at(1), 'Student');
    await tester.enterText(fields.at(2), 'novi@enote.com');
    await tester.tap(find.text('Sačuvaj promjene'));
    await tester.pumpAndSettle();

    final puts = harness.client.jsonRequests
        .where((r) => r.method == 'PUT' && r.url.path == '/api/v1/users/me')
        .toList();
    expect(puts, hasLength(1));
    final body = jsonDecode(puts.single.body) as Map<String, dynamic>;
    expect(body, {
      'email': 'novi@enote.com',
      'firstName': 'Novi',
      'lastName': 'Student',
      'dateOfBirth': '2001-05-12T00:00:00.000',
    });
    expect(find.text('Profil je uspješno ažuriran.'), findsOneWidget);
  });
}
