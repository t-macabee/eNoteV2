import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

void main() {
  group('isMembershipActive', () {
    test('null paidUntil is never active', () {
      expect(
        isMembershipActive(null, now: DateTime.utc(2026, 9, 14, 12)),
        isFalse,
      );
    });

    test('the expiry day is inclusive', () {
      final rows = <(DateTime?, DateTime, bool)>[
        (DateTime.utc(2026, 9, 13), DateTime.utc(2026, 9, 13, 23, 59, 59), true),
        (DateTime.utc(2026, 9, 14), DateTime.utc(2026, 9, 13, 23, 59, 59), true),
        (DateTime.utc(2026, 9, 14), DateTime.utc(2026, 9, 14, 0, 0, 0), true),
        (DateTime.utc(2026, 9, 14), DateTime.utc(2026, 9, 14, 12, 0, 0), true),
        (
          DateTime.utc(2026, 9, 14),
          DateTime.utc(2026, 9, 14, 23, 59, 59, 999),
          true,
        ),
        (DateTime.utc(2026, 9, 14), DateTime.utc(2026, 9, 15, 0, 0, 0), false),
        (DateTime.utc(2026, 9, 14), DateTime.utc(2026, 9, 15, 12, 0, 0), false),
        (DateTime.utc(2020, 3, 4), DateTime.utc(2026, 9, 14, 12, 0, 0), false),
      ];

      for (final row in rows) {
        expect(
          isMembershipActive(row.$1, now: row.$2),
          row.$3,
          reason: 'paidUntil=${row.$1} now=${row.$2}',
        );
      }
    });

    test('a DST transition date does not shift the boundary', () {
      final paidUntil = DateTime.utc(2026, 10, 25);
      expect(
        isMembershipActive(
          paidUntil,
          now: DateTime.utc(2026, 10, 25, 23, 59, 59),
        ),
        isTrue,
        reason: 'the 25-hour fall-back day is still the expiry day',
      );
      expect(
        isMembershipActive(paidUntil, now: DateTime.utc(2026, 10, 26)),
        isFalse,
        reason: 'the next calendar day is expired',
      );
    });

    test('defaults now to the current instant', () {
      expect(isMembershipActive(DateTime.utc(2999, 1, 1)), isTrue);
      expect(isMembershipActive(DateTime.utc(2000, 1, 1)), isFalse);
    });
  });
}
