import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../shell/shell_navigation.dart';
import '../../../widgets/dashboard_count.dart';
import '../../../widgets/dashboard_page.dart';
import '../course/admin_course_provider.dart';
import '../event/event_provider.dart';
import '../music_store/music_store_provider.dart';
import '../users/admin_user_provider.dart';

class AdminDashboardScreen extends StatefulWidget {
  const AdminDashboardScreen({super.key});

  @override
  State<AdminDashboardScreen> createState() => _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends State<AdminDashboardScreen> {
  final _usersCount = DashboardCount();
  final _storesCount = DashboardCount();
  final _coursesCount = DashboardCount();
  final _eventsCount = DashboardCount();

  @override
  void initState() {
    super.initState();
    _usersCount.load(() async {
      final page = await context.read<AdminUserProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
    _storesCount.load(() async {
      final page = await context.read<MusicStoreProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
    _coursesCount.load(() async {
      final page = await context.read<AdminCourseProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
    _eventsCount.load(() async {
      final page = await context.read<EventProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      return page.totalCount;
    });
  }

  @override
  void dispose() {
    _usersCount.dispose();
    _storesCount.dispose();
    _coursesCount.dispose();
    _eventsCount.dispose();
    super.dispose();
  }

  void _openEntry(String label) {
    ShellNavigation.of(context).openEntry(label);
  }

  @override
  Widget build(BuildContext context) {
    return DashboardPage(
      cards: [
        DashboardCountCard(
          count: _usersCount,
          icon: Icons.people_outline,
          label: 'Korisnici',
          tone: StatusTone.info,
          onTap: () => _openEntry('Korisnici'),
        ),
        DashboardCountCard(
          count: _storesCount,
          icon: Icons.store_outlined,
          label: 'Trgovina muzičke opreme',
          tone: StatusTone.accent,
          onTap: () => _openEntry('Trgovina muzičke opreme'),
        ),
        DashboardCountCard(
          count: _coursesCount,
          icon: Icons.class_outlined,
          label: 'Kursevi',
          tone: StatusTone.success,
          onTap: () => _openEntry('Kursevi'),
        ),
        DashboardCountCard(
          count: _eventsCount,
          icon: Icons.event_outlined,
          label: 'Događaji',
          tone: StatusTone.warning,
          onTap: () => _openEntry('Događaji'),
        ),
      ],
      shortcuts: [
        DashboardShortcut(
          label: 'Korisnici',
          icon: Icons.people_outline,
          onTap: () => _openEntry('Korisnici'),
        ),
        DashboardShortcut(
          label: 'Kursevi',
          icon: Icons.class_outlined,
          onTap: () => _openEntry('Kursevi'),
        ),
        DashboardShortcut(
          label: 'Događaji',
          icon: Icons.event_outlined,
          onTap: () => _openEntry('Događaji'),
        ),
      ],
    );
  }
}
