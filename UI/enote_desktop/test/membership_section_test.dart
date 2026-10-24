import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';
import 'package:enote_desktop/features/admin/student/student_provider.dart';
import 'package:enote_desktop/features/shared/membership/membership_section.dart';

import 'helpers.dart';

ScriptedClient _client() => ScriptedClient((request) {
  if (request.url.path.endsWith('/membership-payments')) {
    return jsonResponse({
      'items': [
        {
          'id': 7,
          'studentId': 5,
          'amount': 40.0,
          'paidAt': '2026-09-01T00:00:00Z',
          'periodStart': '2026-09-01T00:00:00Z',
          'periodEnd': '2027-01-01T00:00:00Z',
          'receiptNumber': 'CL-7',
          'note': 'Gotovina',
        },
      ],
      'page': 1,
      'pageSize': 5,
      'totalCount': 1,
    }, 200);
  }
  return jsonResponse({'message': 'nema'}, 404);
});

Future<StudentProvider> _pumpSection(
  WidgetTester tester, {
  required DateTime? membershipPaidUntil,
  required ScriptedClient client,
}) async {
  final apiClient = ApiClient(
    baseUrl: 'http://localhost:5059/api/v1/',
    authState: AuthState(),
    httpClient: client,
  );
  final provider = StudentProvider(apiClient: apiClient);

  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: MembershipSection(
            api: provider,
            studentId: 5,
            membershipPaidUntil: membershipPaidUntil,
            onMembershipChanged: (_) {},
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();

  return provider;
}

void main() {
  testWidgets('E1-10: shows the membership status, history and buttons',
      (tester) async {
    await _pumpSection(
      tester,
      membershipPaidUntil: DateTime.now().add(const Duration(days: 60)),
      client: _client(),
    );

    expect(find.text('Članarina'), findsOneWidget);
    expect(find.text('Aktivna'), findsOneWidget);
    expect(find.textContaining('Potvrda CL-7'), findsOneWidget);
    expect(find.textContaining('Gotovina'), findsOneWidget);

    final warningButton = tester.widget<OutlinedButton>(
      find.widgetWithText(OutlinedButton, 'Pošalji upozorenje'),
    );
    expect(warningButton.onPressed, isNull);
    expect(
      find.byTooltip(
        'Upozorenje je moguće poslati kada članarina ističe u narednih 7 dana ili je već istekla.',
      ),
      findsOneWidget,
    );
  });

  testWidgets('E1-10: the warning button is enabled when membership expires soon',
      (tester) async {
    await _pumpSection(
      tester,
      membershipPaidUntil: DateTime.now().add(const Duration(days: 3)),
      client: _client(),
    );

    final warningButton = tester.widget<OutlinedButton>(
      find.widgetWithText(OutlinedButton, 'Pošalji upozorenje'),
    );
    expect(warningButton.onPressed, isNotNull);
  });

  testWidgets('E1-10: the receipt dialog validates every field under the field',
      (tester) async {
    await _pumpSection(
      tester,
      membershipPaidUntil: null,
      client: _client(),
    );

    await tester.tap(find.text('Unesi potvrdu'));
    await tester.pumpAndSettle();

    expect(find.text('Unesi potvrdu o članarini'), findsOneWidget);

    await tester.tap(find.text('Evidentiraj'));
    await tester.pumpAndSettle();

    expect(find.text('Unesite iznos, npr. 50.00.'), findsOneWidget);
    expect(find.text('Broj potvrde je obavezan.'), findsOneWidget);
    expect(find.text('Kraj perioda je obavezan.'), findsOneWidget);
  });
}
