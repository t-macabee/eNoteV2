import 'package:flutter/material.dart';

import 'package:enote_core/enote_core.dart';

import 'assignment_labels.dart';

class AssignmentTile extends StatelessWidget {
  final AssignmentDto assignment;
  final VoidCallback onTap;

  const AssignmentTile({
    super.key,
    required this.assignment,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final status = assignmentStatus(
      dueAt: assignment.dueAt,
      submittedAt: assignment.mySubmittedAt,
      grade: assignment.myGrade,
      now: DateTime.now(),
    );
    return ListTile(
      onTap: onTap,
      leading: const Icon(Icons.assignment_outlined),
      title: Text(assignment.title),
      subtitle: Text('Rok: ${formatDateTime(assignment.dueAt)}'),
      trailing: StatusChip(label: status.label, tone: status.tone),
    );
  }
}
