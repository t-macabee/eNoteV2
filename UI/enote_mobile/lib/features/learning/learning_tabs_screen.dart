import 'package:flutter/material.dart';

import '../announcements/announcement_list_screen.dart';
import '../assignments/assignment_list_screen.dart';
import '../courses/course_list_screen.dart';
import '../events/event_list_screen.dart';
import '../lectures/lecture_list_screen.dart';

/// S13 — the Učenje tab root: scrollable top `TabBar` over the academic
/// lists (02 §5 S13). Every tab body keeps its state with
/// `AutomaticKeepAliveClientMixin`.
class LearningTabsScreen extends StatelessWidget {
  const LearningTabsScreen({super.key});

  static const _tabs = [
    'Kursevi',
    'Predavanja',
    'Zadaci',
    'Objave',
    'Događaji',
  ];

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: _tabs.length,
      child: Column(
        children: [
          TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [for (final tab in _tabs) Tab(text: tab)],
          ),
          const Expanded(
            child: TabBarView(
              children: [
                CourseListScreen(),
                LectureListScreen(),
                AssignmentListScreen(),
                AnnouncementListScreen(),
                EventListScreen(),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
