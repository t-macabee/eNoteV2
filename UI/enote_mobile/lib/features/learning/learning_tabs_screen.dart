import 'package:flutter/material.dart';

import '../courses/course_list_screen.dart';
import '../lectures/lecture_list_screen.dart';

/// S13 — the Učenje tab root: scrollable top `TabBar` over the academic
/// lists (02 §5 S13). *Zadaci*, *Objave* and *Događaji* are placeholders
/// until Phase 8.
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
                _PlaceholderTab(label: 'Zadaci'),
                _PlaceholderTab(label: 'Objave'),
                _PlaceholderTab(label: 'Događaji'),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PlaceholderTab extends StatelessWidget {
  final String label;

  const _PlaceholderTab({required this.label});

  @override
  Widget build(BuildContext context) {
    return Center(child: Text(label));
  }
}
