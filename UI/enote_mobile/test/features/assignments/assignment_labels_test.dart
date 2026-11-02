import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/features/assignments/assignment_labels.dart';

void main() {
  test('past due assignment is Rok istekao with danger', () {
    final status = assignmentDeadlineStatus(
      DateTime(2026, 10, 1),
      DateTime(2026, 11, 1),
    );
    expect(status.label, 'Rok istekao');
    expect(status.tone, StatusTone.danger);
  });

  test('open assignment is Otvoreno with info', () {
    final status = assignmentDeadlineStatus(
      DateTime(2026, 11, 30),
      DateTime(2026, 11, 1),
    );
    expect(status.label, 'Otvoreno');
    expect(status.tone, StatusTone.info);
  });
}
