import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/formatting/formatters.dart';

void main() {
  group('formatKM', () {
    test('null renders an em dash', () {
      expect(formatKM(null), '—');
    });

    test('zero renders with KM suffix', () {
      expect(formatKM(0), '0.00 KM');
    });

    test('rounds to two decimals', () {
      expect(formatKM(2.345), '2.35 KM');
      expect(formatKM(2.344), '2.34 KM');
    });

    test('regular amount', () {
      expect(formatKM(50), '50.00 KM');
    });

    test('bam currency renders as KM', () {
      expect(formatKM(50, currency: 'bam'), '50.00 KM');
    });

    test('empty currency falls back to KM', () {
      expect(formatKM(50, currency: ''), '50.00 KM');
    });

    test('other currency is upper-cased', () {
      expect(formatKM(50, currency: 'eur'), '50.00 EUR');
    });

    test('default currency stays KM', () {
      expect(formatKM(50), '50.00 KM');
    });
  });

  group('orDash', () {
    test('null and empty render an em dash', () {
      expect(orDash(null), '—');
      expect(orDash(''), '—');
    });

    test('passes other strings through', () {
      expect(orDash('Sarajevo'), 'Sarajevo');
    });
  });

  group('toUtcIso and parseDate', () {
    test('toUtcIso emits ISO-8601 string in UTC', () {
      final local = DateTime(2026, 9, 21, 14, 30);
      final wire = toUtcIso(local);
      expect(wire, endsWith('Z'));
      expect(DateTime.parse(wire).isUtc, isTrue);
    });

    test('parseDate normalizes UTC ISO string to local DateTime', () {
      const utcIso = '2026-09-21T12:00:00Z';
      final parsed = parseDate(utcIso);
      expect(parsed, isNotNull);
      expect(parsed!.isUtc, isFalse);
      expect(parsed, DateTime.parse(utcIso).toLocal());
    });

    test('parseDate normalizes UTC DateTime object to local DateTime', () {
      final utcDate = DateTime.utc(2026, 9, 21, 12, 0);
      final parsed = parseDate(utcDate);
      expect(parsed, isNotNull);
      expect(parsed!.isUtc, isFalse);
      expect(parsed, utcDate.toLocal());
    });

    test('parseDate round-trips instant with toUtcIso', () {
      final originalLocal = DateTime(2026, 9, 21, 19, 30);
      final wire = toUtcIso(originalLocal);
      final restored = parseDate(wire);
      expect(restored, isNotNull);
      expect(restored!.isAtSameMomentAs(originalLocal), isTrue);
    });

    test('parseDate returns null on invalid or null input', () {
      expect(parseDate(null), isNull);
      expect(parseDate('not-a-date'), isNull);
      expect(parseDate(12345), isNull);
    });
  });
}
