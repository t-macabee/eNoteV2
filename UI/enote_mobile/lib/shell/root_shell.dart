import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../features/home/home_screen.dart';
import '../features/instruments/instruments_tabs_screen.dart';
import '../features/learning/learning_tabs_screen.dart';
import '../features/profile/profile_screen.dart';
import '../realtime/notification_hub_client.dart';
import '../session/session_controller.dart';
import 'routes.dart';

enum RootTab {
  home('Početna', Icons.home_outlined, Icons.home),
  learning('Učenje', Icons.school_outlined, Icons.school),
  instruments('Instrumenti', Icons.piano_outlined, Icons.piano),
  profile('Profil', Icons.person_outline, Icons.person);

  const RootTab(this.title, this.icon, this.selectedIcon);

  final String title;
  final IconData icon;
  final IconData selectedIcon;
}

class RootShell extends StatefulWidget {
  static final GlobalKey<RootShellState> shellKey = GlobalKey<RootShellState>();

  final RouteFactory onGenerateRoute;

  const RootShell({super.key, required this.onGenerateRoute});

  static void routeNotification(NotificationDto notification) {
    shellKey.currentState?.routeNotification(notification);
  }

  @override
  State<RootShell> createState() => RootShellState();
}

class RootShellState extends State<RootShell> with WidgetsBindingObserver {
  RootTab _tab = RootTab.home;
  bool _booted = false;
  late final List<GlobalKey<NavigatorState>> _navKeys;
  NotificationController? _notifications;
  NotificationHubClient? _hub;

  @override
  void initState() {
    super.initState();
    _navKeys = List.generate(
      RootTab.values.length,
      (_) => GlobalKey<NavigatorState>(),
    );
    WidgetsBinding.instance.addObserver(this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!_booted) {
        _booted = true;
        if (mounted) {
          unawaited(context.read<SessionController>().bootstrap());
        }
      }
    });
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final notifications = context.read<NotificationController>();
    _notifications = notifications;
    notifications.startPolling();
    final hub = context.read<NotificationHubClient>();
    _hub = hub;
    hub.onRefresh = () {
      notifications.refresh();
    };
    hub.onPush = _showPushSnack;
    final auth = context.read<AuthState>();
    if (auth.isAuthenticated && auth.hasRole('Student')) {
      hub.start();
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _notifications?.stopPolling();
    if (_hub != null) {
      _hub!.onRefresh = () {};
      _hub!.onPush = (_) {};
      unawaited(_hub!.stop());
    }
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    final hub = context.read<NotificationHubClient>();
    if (state == AppLifecycleState.paused) {
      hub.stop();
    } else if (state == AppLifecycleState.resumed) {
      final auth = context.read<AuthState>();
      if (!mounted) return;
      if (auth.isAuthenticated && auth.hasRole('Student')) {
        hub.start();
      }
    }
  }

  void _showPushSnack(NotificationPushDto push) {
    if (!mounted) return;
    final messenger = ScaffoldMessenger.of(context);
    messenger.clearSnackBars();
    messenger.showSnackBar(
      SnackBar(
        content: Text(push.title),
        duration: const Duration(seconds: 4),
        persist: false,
        behavior: SnackBarBehavior.floating,
        action: SnackBarAction(
          label: 'Prikaži',
          onPressed: () => Navigator.of(
            context,
            rootNavigator: true,
          ).pushNamed(AppRoutes.notifications),
        ),
      ),
    );
  }

  void routeNotification(NotificationDto notification) {
    final RootTab tab;
    final String route;
    final Object? args;
    if (notification.rentalId != null) {
      tab = RootTab.instruments;
      route = AppRoutes.rentalDetail;
      args = RentalDetailArgs(notification.rentalId!);
    } else if (notification.lectureId != null) {
      tab = RootTab.learning;
      route = AppRoutes.lectureDetail;
      args = LectureDetailArgs(notification.lectureId!);
    } else if (notification.submissionId != null) {
      tab = RootTab.learning;
      route = AppRoutes.assignmentHistory;
      args = null;
    } else if (notification.announcementId != null) {
      tab = RootTab.learning;
      route = AppRoutes.announcementDetail;
      args = AnnouncementDetailArgs(notification.announcementId!);
    } else if (notification.courseId != null) {
      tab = RootTab.learning;
      route = AppRoutes.courseDetail;
      args = CourseDetailArgs(notification.courseId!);
    } else {
      return;
    }
    setState(() => _tab = tab);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      _navKeys[tab.index].currentState?.pushNamed(route, arguments: args);
    });
  }

  void _selectDestination(RootTab tab) {
    if (tab == _tab) {
      _navKeys[tab.index].currentState?.popUntil((route) => route.isFirst);
      return;
    }
    setState(() => _tab = tab);
  }

  Future<void> _handlePop() async {
    final navigator = _navKeys[_tab.index].currentState;
    if (navigator != null && navigator.canPop()) {
      navigator.pop();
      return;
    }
    if (_tab != RootTab.home) {
      setState(() => _tab = RootTab.home);
      return;
    }
    await SystemNavigator.pop();
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) {
          _handlePop();
        }
      },
      child: Scaffold(
        body: IndexedStack(
          index: _tab.index,
          children: [
            for (final tab in RootTab.values)
              Navigator(
                key: _navKeys[tab.index],
                onGenerateRoute: (settings) {
                  if (settings.name == null || settings.name == '/') {
                    return MaterialPageRoute(
                      builder: (_) => _TabRoot(tab: tab),
                    );
                  }
                  return widget.onGenerateRoute(settings);
                },
              ),
          ],
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: _tab.index,
          onDestinationSelected: (value) =>
              _selectDestination(RootTab.values[value]),
          destinations: [
            for (final tab in RootTab.values)
              NavigationDestination(
                icon: Icon(tab.icon),
                selectedIcon: Icon(tab.selectedIcon),
                label: tab.title,
              ),
          ],
        ),
      ),
    );
  }
}

class _TabRoot extends StatelessWidget {
  final RootTab tab;

  const _TabRoot({required this.tab});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(tab.title),
        actions: [
          NotificationBadge(
            controller: context.read<NotificationController>(),
            onTap: () => Navigator.of(
              context,
              rootNavigator: true,
            ).pushNamed(AppRoutes.notifications),
          ),
        ],
      ),
      body: switch (tab) {
        RootTab.home => const HomeScreen(),
        RootTab.learning => const LearningTabsScreen(),
        RootTab.instruments => const InstrumentsTabsScreen(),
        RootTab.profile => const ProfileScreen(),
      },
    );
  }
}
