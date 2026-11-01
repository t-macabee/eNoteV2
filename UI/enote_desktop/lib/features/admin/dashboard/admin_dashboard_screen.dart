import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../../shell/shell_navigation.dart';
import '../../../widgets/dashboard_page.dart';
import '../../../widgets/dashboard_stat_card.dart';
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
  int? _usersCount;
  int? _storesCount;
  int? _coursesCount;
  int? _eventsCount;

  bool _usersLoading = true;
  bool _storesLoading = true;
  bool _coursesLoading = true;
  bool _eventsLoading = true;

  bool _usersFailed = false;
  bool _storesFailed = false;
  bool _coursesFailed = false;
  bool _eventsFailed = false;

  @override
  void initState() {
    super.initState();
    _loadUsers();
    _loadStores();
    _loadCourses();
    _loadEvents();
  }

  Future<void> _loadUsers() async {
    try {
      final page = await context.read<AdminUserProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _usersCount = page.totalCount;
        _usersLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _usersFailed = true;
        _usersLoading = false;
      });
    }
  }

  Future<void> _loadStores() async {
    try {
      final page = await context.read<MusicStoreProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _storesCount = page.totalCount;
        _storesLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _storesFailed = true;
        _storesLoading = false;
      });
    }
  }

  Future<void> _loadCourses() async {
    try {
      final page = await context.read<AdminCourseProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _coursesCount = page.totalCount;
        _coursesLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _coursesFailed = true;
        _coursesLoading = false;
      });
    }
  }

  Future<void> _loadEvents() async {
    try {
      final page = await context.read<EventProvider>().getPage(
        params: pagedQuery(1, 1, ''),
      );
      if (!mounted) return;
      setState(() {
        _eventsCount = page.totalCount;
        _eventsLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _eventsFailed = true;
        _eventsLoading = false;
      });
    }
  }

  void _openEntry(String label) {
    ShellNavigation.of(context).openEntry(label);
  }

  @override
  Widget build(BuildContext context) {
    return DashboardPage(
      cards: [
        DashboardStatCard(
          icon: Icons.people_outline,
          label: 'Korisnici',
          value: _usersCount,
          isLoading: _usersLoading,
          hasError: _usersFailed,
          tone: StatusTone.info,
          onTap: () => _openEntry('Korisnici'),
        ),
        DashboardStatCard(
          icon: Icons.store_outlined,
          label: 'Trgovina muzičke opreme',
          value: _storesCount,
          isLoading: _storesLoading,
          hasError: _storesFailed,
          tone: StatusTone.accent,
          onTap: () => _openEntry('Trgovina muzičke opreme'),
        ),
        DashboardStatCard(
          icon: Icons.class_outlined,
          label: 'Kursevi',
          value: _coursesCount,
          isLoading: _coursesLoading,
          hasError: _coursesFailed,
          tone: StatusTone.success,
          onTap: () => _openEntry('Kursevi'),
        ),
        DashboardStatCard(
          icon: Icons.event_outlined,
          label: 'Događaji',
          value: _eventsCount,
          isLoading: _eventsLoading,
          hasError: _eventsFailed,
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
