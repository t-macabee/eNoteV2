import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import '../../../widgets/status_filter_tabs.dart';
import '../../../widgets/workflow_actions.dart';

import 'instructor_enrollment_provider.dart';

enum EnrollmentAction { approve, reject, complete }

String enrollmentSuccessMessage(EnrollmentAction action) => switch (action) {
  EnrollmentAction.approve => 'Upis odobren.',
  EnrollmentAction.reject => 'Upis odbijen.',
  EnrollmentAction.complete => 'Upis označen kao položen.',
};

Future<void> runEnrollmentAction(
  InstructorEnrollmentProvider provider,
  CourseEnrollmentDto item,
  EnrollmentAction action,
  String? note,
) async {
  switch (action) {
    case EnrollmentAction.approve:
      await provider.approve(item);
    case EnrollmentAction.reject:
      await provider.reject(item, note ?? '');
    case EnrollmentAction.complete:
      await provider.complete(item);
  }
}

List<StatusFilterOption<EnrollmentStatus?>> enrollmentStatusFilterOptions({
  int? pendingCount,
}) => [
  StatusFilterOption(
    label: enrollmentStatusLabel(EnrollmentStatus.pending),
    value: EnrollmentStatus.pending,
    count: pendingCount,
  ),
  for (final status in EnrollmentStatus.values)
    if (status != EnrollmentStatus.pending)
      StatusFilterOption(label: enrollmentStatusLabel(status), value: status),
  const StatusFilterOption(label: 'Svi', value: null),
];

List<WorkflowAction<EnrollmentAction>> enrollmentActions(
  CourseEnrollmentDto item,
) => switch (item.enrollmentStatus) {
  EnrollmentStatus.pending => [
    WorkflowAction(
      trigger: EnrollmentAction.approve,
      label: 'Odobri',
      icon: Icons.check_circle_outline,
      tone: StatusTone.success,
      dialogTitle: 'Odobri upis',
      confirmMessage: 'Odobriti upis za studenta ${item.studentName}?',
    ),
    const WorkflowAction(
      trigger: EnrollmentAction.reject,
      label: 'Odbij',
      icon: Icons.cancel_outlined,
      tone: StatusTone.danger,
      requiresNote: true,
      dialogTitle: 'Odbij upis',
    ),
  ],
  EnrollmentStatus.active => [
    WorkflowAction(
      trigger: EnrollmentAction.complete,
      label: 'Položio',
      icon: Icons.school_outlined,
      dialogTitle: 'Označi kao položen',
      confirmMessage:
          'Označiti da je student ${item.studentName} položio kurs? '
          'Student time gubi pristup sadržaju kursa.',
    ),
  ],
  _ => const [],
};
