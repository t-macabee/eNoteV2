import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../theme/app_theme.dart';
import '../../../widgets/dialog_shell.dart';
import '../../../widgets/inline_error_text.dart';
import 'student_directory_provider.dart';

class StudentProfileDialog extends StatefulWidget {
  final StudentDirectoryEntryDto student;

  const StudentProfileDialog({super.key, required this.student});

  @override
  State<StudentProfileDialog> createState() => _StudentProfileDialogState();
}

class _StudentProfileDialogState extends State<StudentProfileDialog> {
  StudentProfileDto? _profile;
  bool _error = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final profile = await context.read<StudentDirectoryProvider>().getProfile(
        widget.student.id,
      );
      if (mounted) {
        setState(() {
          _profile = profile;
          _error = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final profile = _profile;

    return DialogShell(
      title: 'Profil studenta',
      width: DialogShellWidth.md,
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (_error)
              const InlineErrorText('Greška pri učitavanju profila studenta.')
            else if (profile == null)
              const SizedBox(
                height: 120,
                child: Center(child: CircularProgressIndicator()),
              )
            else ...[
              InfoRow(
                icon: Icons.person_outline,
                label: 'Ime i prezime',
                value: formatDisplayName(
                  profile.firstName,
                  profile.lastName,
                  profile.username,
                ),
              ),
              InfoRow(
                icon: Icons.alternate_email,
                label: 'Korisničko ime',
                value: profile.username != null ? '@${profile.username}' : '-',
              ),
              const SizedBox(height: 16),
              const Text(
                'Kursevi',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppTheme.textPrimary,
                ),
              ),
              const SizedBox(height: 8),
              if (profile.courses.isEmpty)
                const Text(
                  'Student nema kurseva.',
                  style: TextStyle(fontSize: 13, color: AppTheme.textSecondary),
                )
              else
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: DataTable(
                    columns: const [
                      DataColumn(label: Text('Kurs')),
                      DataColumn(label: Text('Instruktor')),
                      DataColumn(label: Text('Status')),
                      DataColumn(label: Text('Prosjek')),
                      DataColumn(label: Text('Ocijenjeno')),
                    ],
                    rows: [
                      for (final course in profile.courses)
                        DataRow(
                          cells: [
                            DataCell(Text(course.courseName)),
                            DataCell(Text(orDash(course.instructorName))),
                            DataCell(
                              Text(
                                enrollmentStatusLabel(course.enrollmentStatus),
                              ),
                            ),
                            DataCell(
                              Text(formatAverageGrade(course.averageGrade)),
                            ),
                            DataCell(Text(course.gradedSubmissions.toString())),
                          ],
                        ),
                    ],
                  ),
                ),
            ],
          ],
        ),
      ),
    );
  }
}
