import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/detail_row.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/entity_list_screen.dart';
import '../../shared/membership/membership_section.dart';
import '../lecture/lecture_provider.dart';
import '../lecture_note/lecture_note_list_screen.dart';
import '../lecture_note/lecture_note_provider.dart';
import 'instructor_student_provider.dart';

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
  late DateTime? _membershipPaidUntil = widget.student.membershipPaidUntil;
  List<StudentEnrollmentDto>? _enrollments;
  bool _enrollmentsError = false;
  List<LectureDto>? _individualLectures;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    final provider = context.read<InstructorStudentProvider>();
    final lectureProvider = context.read<LectureProvider>();
    try {
      final fresh = await provider.getById(widget.student.id);
      if (mounted && fresh.id == _student.id) {
        setState(() {
          _student = fresh;
          _membershipPaidUntil = fresh.membershipPaidUntil;
        });
      }
    } catch (_) {
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

    try {
      final lectures = await lectureProvider.search(
            pagedQuery(
              1,
              100,
              '',
              filters: {'studentId': widget.student.id},
            ),
          );
      if (mounted) {
        setState(() => _individualLectures = lectures.items);
      }
    } catch (_) {
      if (mounted) {
        setState(() => _individualLectures = const []);
      }
    }
  }

  Future<void> _openNotes(LectureDto lecture) async {
    final apiClient = context.read<ApiClient>();
    await showDialog<void>(
      context: context,
      builder: (_) => ChangeNotifierProvider<LectureNoteProvider>(
        create: (_) => LectureNoteProvider(
          apiClient: apiClient,
          lectureId: lecture.id,
        ),
        child: DialogShell(
          title: 'Bilješke — ${lecture.name}',
          width: DialogShellWidth.md,
          body: LectureNoteListScreen(
            lectureId: lecture.id,
            lectureName: lecture.name,
            presentation: EntityListPresentation.embedded,
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final student = _student;

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
            const SizedBox(height: 8),
            MembershipSection(
              api: context.read<InstructorStudentProvider>(),
              studentId: student.id,
              membershipPaidUntil: _membershipPaidUntil,
              onMembershipChanged: (paidUntil) =>
                  setState(() => _membershipPaidUntil = paidUntil),
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
            const SizedBox(height: 16),
            const Text(
              'Individualni plan',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: AppTheme.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            if (_individualLectures == null)
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
            else if (_individualLectures!.isEmpty)
              const Text(
                'Nema individualnih predavanja.',
                style: TextStyle(
                  fontSize: 13,
                  color: AppTheme.textSecondary,
                ),
              )
            else
              ..._individualLectures!.map(
                (lecture) => ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.person_outline),
                  title: Text(lecture.name),
                  subtitle: Text(
                    '${formatDateTime(lecture.lectureTime)} · ${lecture.location}',
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppTheme.textSecondary,
                    ),
                  ),
                  trailing: TextButton(
                    onPressed: () => _openNotes(lecture),
                    child: const Text('Bilješke'),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
