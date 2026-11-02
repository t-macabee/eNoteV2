import 'package:flutter/material.dart';

class SectionTabs extends StatelessWidget {
  final List<({String label, Widget child})> tabs;

  const SectionTabs({super.key, required this.tabs});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: tabs.length,
      child: Column(
        children: [
          TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [for (final tab in tabs) Tab(text: tab.label)],
          ),
          Expanded(
            child: TabBarView(children: [for (final tab in tabs) tab.child]),
          ),
        ],
      ),
    );
  }
}
