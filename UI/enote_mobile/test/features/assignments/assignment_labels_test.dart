import 'package:flutter_test/flutter_test.dart';

import 'package:enote_core/enote_core.dart';

import 'package:enote_mobile/features/assignments/assignment_labels.dart';

void main() {
  final now = DateTime(2026, 11, 1);

  test('a graded assignment shows the grade with success', () {
    final status = assignmentStatus(
      dueAt: DateTime(2026, 10, 1),
      submittedAt: DateTime(2026, 9, 30),
      grade: 85,
      now: now,
    );
    expect(status.label, 'Ocjena: 85');
    expect(status.tone, StatusTone.success);
  });

  test('a submitted assignment shows Predano with info', () {
    final status = assignmentStatus(
      dueAt: DateTime(2026, 11, 30),
      submittedAt: DateTime(2026, 10, 30),
      now: now,
    );
    expect(status.label, 'Predano');
    expect(status.tone, StatusTone.info);
  });

  test('an unsubmitted assignment past its due date is Rok istekao', () {
    final status = assignmentStatus(dueAt: DateTime(2026, 10, 1), now: now);
    expect(status.label, 'Rok istekao');
    expect(status.tone, StatusTone.danger);
  });

  test('an unsubmitted assignment before its due date is Nije predano', () {
    final status = assignmentStatus(dueAt: DateTime(2026, 11, 30), now: now);
    expect(status.label, 'Nije predano');
    expect(status.tone, StatusTone.warning);
  });
}
