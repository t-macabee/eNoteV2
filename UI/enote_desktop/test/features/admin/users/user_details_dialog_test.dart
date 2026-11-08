import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/student/student_provider.dart';
import 'package:enote_desktop/features/admin/users/admin_user_provider.dart';
import 'package:enote_desktop/features/admin/users/user_details_dialog.dart';
import 'package:enote_desktop/theme/app_theme.dart';
import 'package:enote_desktop/widgets/dialog_shell.dart';

import '../../../helpers.dart';

const _emptyPage = {'items': [], 'page': 1, 'pageSize': 5, 'totalCount': 0};

ScriptedClient _client() => ScriptedClient((request) {
  if (request.url.path.endsWith('/membership-payments')) {
    return jsonResponse(_emptyPage, 200);
  }
  return jsonResponse({
    'role': 'Student',
    'username': 'lbeethoven',
    'email': 'l@example.com',
    'profile': {
      'id': 2,
      'firstName': 'Ludwig',
      'lastName': 'Beethoven',
      'enrollmentDate': '2026-01-10T00:00:00Z',
      'membershipPaidUntil': '2026-12-31T00:00:00Z',
    },
  }, 200);
});

Future<void> _pumpDialog(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1280, 2000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final client = _client();
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: client,
  );

  final item = AdminUserListItemDto.fromJson(const {
    'appUserId': 22,
    'username': 'lbeethoven',
    'firstName': 'Ludwig',
    'lastName': 'Beethoven',
    'role': 'Student',
    'isActive': true,
    'studentId': 2,
    'enrollmentDate': '2026-01-10T00:00:00Z',
    'membershipPaidUntil': '2026-12-31T00:00:00Z',
  });

  await tester.pumpWidget(
    MultiProvider(
      providers: [
        Provider<ApiClient>.value(value: apiClient),
        ChangeNotifierProvider(
          create: (_) => AdminUserProvider(apiClient: apiClient),
        ),
        ChangeNotifierProvider(
          create: (_) => StudentProvider(apiClient: apiClient),
        ),
      ],
      child: MaterialApp(
        theme: desktopTheme,
        home: Scaffold(
          body: UserDetailsDialog(
            item: item,
            onStatusChange: (_) async => true,
            onDelete: () async => true,
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'the membership heading has the section gap above it for a student',
    (tester) async {
      await _pumpDialog(tester);

      final enrollmentRowBottom = tester
          .getRect(find.widgetWithText(InfoRow, 'Datum upisa'))
          .bottom;
      final membershipHeadingTop = tester.getRect(find.text('Članarina')).top;

      expect(membershipHeadingTop - enrollmentRowBottom, AppSpacing.lg);
    },
  );

  testWidgets('the user dialog uses the small dialog width', (tester) async {
    await _pumpDialog(tester);

    final panel = find
        .descendant(of: find.byType(Dialog), matching: find.byType(Material))
        .first;

    expect(tester.getSize(panel).width, DialogShellWidth.sm.value);
  });
}
