import 'package:flutter_test/flutter_test.dart';

import 'package:enote_desktop/features/admin/users/user_grid_screen.dart';

void main() {
  test('F2-02: Jan 31 clamps to the last day of February', () {
    expect(nextMonthClamped(DateTime(2026, 1, 31)), DateTime(2026, 2, 28));
    expect(nextMonthClamped(DateTime(2024, 1, 31)), DateTime(2024, 2, 29));
  });

  test('F2-02: Mar 31 clamps to Apr 30, mid-month passes through', () {
    expect(nextMonthClamped(DateTime(2026, 3, 31)), DateTime(2026, 4, 30));
    expect(nextMonthClamped(DateTime(2026, 1, 15)), DateTime(2026, 2, 15));
  });

  test('F2-02: Dec rolls into January of the next year', () {
    expect(nextMonthClamped(DateTime(2026, 12, 31)), DateTime(2027, 1, 31));
  });
}
