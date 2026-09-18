import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';
import 'package:enote_core/testing/testing.dart';
export 'package:enote_core/testing/testing.dart';

Map<String, dynamic> meJson({DateTime? paidUntil, bool hasPicture = false}) => {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {
    'id': 7,
    'firstName': 'Student',
    'lastName': 'Enote',
    'dateOfBirth': '2001-05-12T00:00:00',
    'membershipPaidUntil':
        (paidUntil ?? DateTime.utc(2027, 9, 9)).toIso8601String(),
  },
  'hasPicture': hasPicture,
};

/// Number of recorded requests matching `'METHOD /path'` (query ignored).
int countRequests(ScriptedClient client, String marker) =>
    client.requests.where((r) => '${r.method} ${r.url.path}' == marker).length;

Future<void> pumpPastDebounce(WidgetTester tester) => tester.pump(
  PagedFetchController.defaultSearchDebounce + const Duration(milliseconds: 100),
);
