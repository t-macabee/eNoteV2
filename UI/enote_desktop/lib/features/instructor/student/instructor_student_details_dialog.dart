import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import 'instructor_student_provider.dart';

/// Read-only student info for instructors — deliberately minimal: no
/// membership renew, activate/deactivate or delete actions (those are admin
/// only), just the facts the instructor needs: who the student is, when they
/// enrolled, and their membership status.
class InstructorStudentDetailsDialog extends StatefulWidget {
  final StudentDto student;

  const InstructorStudentDetailsDialog({super.key, required this.student});

  @override
  State<InstructorStudentDetailsDialog> createState() =>
      _InstructorStudentDetailsDialogState();
}

class _InstructorStudentDetailsDialogState
    extends State<InstructorStudentDetailsDialog> {
  late StudentDto _student = widget.student;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    try {
      final fresh = await context
          .read<InstructorStudentProvider>()
          .getById(widget.student.id);
      if (mounted && fresh.id == _student.id) {
        setState(() => _student = fresh);
      }
    } catch (_) {
      // Ignored: fall back to the details already in the grid item
    }
  }

  @override
  Widget build(BuildContext context) {
    final student = _student;
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final isMembershipActive = student.membershipPaidUntil != null &&
        (student.membershipPaidUntil!.isAfter(today) ||
            student.membershipPaidUntil!.isAtSameMomentAs(today));

    final statusValue = isMembershipActive
        ? 'Aktivna do ${formatDateNullable(student.membershipPaidUntil)}'
        : 'Istekla / Nema';

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Detalji studenta',
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: AppTheme.textPrimary,
                ),
              ),
              const SizedBox(height: 16),
              InfoRow(
                icon: Icons.person_outline,
                label: 'Ime i prezime',
                value: formatDisplayName(
                  student.firstName,
                  student.lastName,
                  student.username,
                ),
              ),
              InfoRow(
                icon: Icons.alternate_email,
                label: 'Korisničko ime',
                value: student.username != null ? '@${student.username}' : '-',
              ),
              InfoRow(
                icon: Icons.calendar_today_outlined,
                label: 'Datum upisa',
                value: formatDateNullable(student.enrollmentDate),
              ),
              InfoRow(
                icon: Icons.card_membership_outlined,
                label: 'Status članarine',
                value: statusValue,
              ),
              const SizedBox(height: 24),
              Row(
                children: [
                  const Spacer(),
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Zatvori'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
