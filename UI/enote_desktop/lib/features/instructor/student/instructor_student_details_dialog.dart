import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';
import 'instructor_student_provider.dart';

/// Read-only student info for instructors — deliberately minimal: no
/// membership renew, activate/deactivate or delete actions (those are admin
/// only), just the facts the instructor needs: who the student is, when they
/// enrolled, their membership status, and cross-enrollments in other courses
/// (shown because the student is already visible to this instructor).
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
  List<StudentEnrollmentDto>? _enrollments;
  bool _enrollmentsError = false;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    final provider = context.read<InstructorStudentProvider>();
    try {
      final fresh = await provider.getById(widget.student.id);
      if (mounted && fresh.id == _student.id) {
        setState(() => _student = fresh);
      }
    } catch (_) {
      // Ignored: fall back to the details already in the grid item
    }

    try {
      final enrollments = await provider.getEnrollments(widget.student.id);
      if (mounted) {
        setState(() {
          _enrollments = enrollments;
          _enrollmentsError = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _enrollmentsError = true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final student = _student;
    final membershipActive = isMembershipActive(student.membershipPaidUntil);

    final statusValue = membershipActive
        ? 'Aktivna do ${formatDateNullable(student.membershipPaidUntil)}'
        : 'Istekla / Nema';

    return DialogShell(
      title: 'Detalji studenta',
      width: DialogShellWidth.sm,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
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
            const SizedBox(height: 16),
            const Text(
              'Upisani kursevi',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: AppTheme.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            if (_enrollmentsError)
              const Text(
                'Greška pri učitavanju upisa.',
                style: TextStyle(
                  fontSize: 13,
                  color: AppTheme.error,
                ),
              )
            else if (_enrollments == null)
              const SizedBox(
                height: 24,
                child: Center(
                  child: SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                ),
              )
            else if (_enrollments!.isEmpty)
              const Text(
                'Nema aktivnih upisa.',
                style: TextStyle(
                  fontSize: 13,
                  color: AppTheme.textSecondary,
                ),
              )
            else
              ..._enrollments!.map(
                (e) => DetailRow(
                  icon: Icons.class_outlined,
                  label: e.courseName,
                  value: e.instructorName ?? '—',
                ),
              ),
          ],
        ),
      ),
    );
  }
}
