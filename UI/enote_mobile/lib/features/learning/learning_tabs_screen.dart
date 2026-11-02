import 'package:flutter/material.dart';

import '../../widgets/section_tabs.dart';
import '../announcements/announcement_list_screen.dart';
import '../assignments/assignment_list_screen.dart';
import '../courses/course_list_screen.dart';
import '../events/event_list_screen.dart';
import '../lectures/lecture_list_screen.dart';

class LearningTabsScreen extends StatelessWidget {
  const LearningTabsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const SectionTabs(
      tabs: [
        (label: 'Kursevi', child: CourseListScreen()),
        (label: 'Predavanja', child: LectureListScreen()),
        (label: 'Zadaci', child: AssignmentListScreen()),
        (label: 'Objave', child: AnnouncementListScreen()),
        (label: 'Događaji', child: EventListScreen()),
      ],
    );
  }
}
