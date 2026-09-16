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
}
