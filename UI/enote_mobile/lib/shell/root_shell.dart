import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../features/instruments/instrument_catalog_screen.dart';
import '../features/profile/profile_screen.dart';
import '../features/rentals/rental_list_screen.dart';
import '../realtime/notification_hub_client.dart';
import '../session/session_controller.dart';
import 'app_router.dart';

class RootShell extends StatefulWidget {
  static final GlobalKey<RootShellState> shellKey =
      GlobalKey<RootShellState>();

  const RootShell({super.key});

  /// Routes a notification payload to its detail screen: switches to the
  /// matching tab, then pushes the target route on that tab's navigator so
  /// the back stack stays sensible (02 §6.1). Called from the inbox (which
  /// lives on the root navigator) after it pops itself.
  static void routeNotification(NotificationDto notification) {
    shellKey.currentState?.routeNotification(notification);
  }

  /// Switches the bottom navigation to [index]. Used after an action on one
  /// tab whose result lives on another — a sent rental request lands the
  /// student on Iznajmljivanja (02 §5, S9).
  static void switchTab(int index) {
    shellKey.currentState?.selectTab(index);
  }

  @override
  State<RootShell> createState() => RootShellState();
}

class RootShellState extends State<RootShell> with WidgetsBindingObserver {
  static const _titles = [
    'Instrumenti',
    'Učenje',
    'Iznajmljivanja',
    'Profil',
  ];

  int _index = 0;
  bool _booted = false;
  late final List<GlobalKey<NavigatorState>> _navKeys;
  NotificationController? _notifications;
  NotificationHubClient? _hub;

  @override
  void initState() {
    super.initState();
    _navKeys = List.generate(4, (_) => GlobalKey<NavigatorState>());
    WidgetsBinding.instance.addObserver(this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!_booted) {
        _booted = true;
        if (mounted) {
          context.read<SessionController>().bootstrap();
        }
      }
    });
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final notifications = context.read<NotificationController>();
    // Saved for dispose(): ancestor lookup is unsafe once deactivated.
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
          ).pushNamed(AppRouter.notifications),
        ),
      ),
    );
  }

  /// Switches to the tab matching [notification]'s payload, then pushes the
  /// target detail route on that tab's navigator (targets are placeholders
  /// until T44/T59/T64). Payload-less notifications are ignored.
  void routeNotification(NotificationDto notification) {
    final int tab;
    final String route;
    final Object? args;
    if (notification.rentalId != null) {
      tab = 2;
      route = AppRouter.rentalDetail;
      args = RentalDetailArgs(notification.rentalId!);
    } else if (notification.lectureId != null) {
      tab = 1;
      route = AppRouter.lectureDetail;
      args = LectureDetailArgs(notification.lectureId!);
    } else if (notification.submissionId != null) {
      tab = 1;
      route = AppRouter.assignmentHistory;
      args = null;
    } else {
      return;
    }
    setState(() => _index = tab);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      _navKeys[tab].currentState?.pushNamed(route, arguments: args);
    });
  }

  /// Selects [index] on the bottom navigation.
  void selectTab(int index) {
    if (!mounted || index == _index) return;
    setState(() => _index = index);
  }

  Future<void> _handlePop() async {
    final navigator = _navKeys[_index].currentState;
    if (navigator != null && navigator.canPop()) {
      navigator.pop();
      return;
    }
    if (_index != 0) {
      setState(() => _index = 0);
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
          index: _index,
          children: [
            for (var i = 0; i < 4; i++)
              Navigator(
                key: _navKeys[i],
                onGenerateRoute: (settings) {
                  if (settings.name == null || settings.name == '/') {
                    return MaterialPageRoute(
                      builder: (_) => _TabRoot(index: i),
                    );
                  }
                  return AppRouter.onGenerateRoute(settings);
                },
              ),
          ],
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: _index,
          onDestinationSelected: (value) => setState(() => _index = value),
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.piano_outlined),
              selectedIcon: Icon(Icons.piano),
              label: 'Instrumenti',
            ),
            NavigationDestination(
              icon: Icon(Icons.school_outlined),
              selectedIcon: Icon(Icons.school),
              label: 'Učenje',
            ),
            NavigationDestination(
              icon: Icon(Icons.receipt_long_outlined),
              selectedIcon: Icon(Icons.receipt_long),
              label: 'Iznajmljivanja',
            ),
            NavigationDestination(
              icon: Icon(Icons.person_outline),
              selectedIcon: Icon(Icons.person),
              label: 'Profil',
            ),
          ],
        ),
      ),
    );
  }
}

class _TabRoot extends StatelessWidget {
  final int index;

  const _TabRoot({required this.index});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(RootShellState._titles[index]),
        actions: [
          NotificationBadge(
            controller: context.read<NotificationController>(),
            onTap: () => Navigator.of(
              context,
              rootNavigator: true,
            ).pushNamed(AppRouter.notifications),
          ),
        ],
      ),
      body: switch (index) {
        0 => const InstrumentCatalogScreen(),
        2 => const RentalListScreen(),
        3 => const ProfileScreen(),
        _ => Center(child: Text(RootShellState._titles[index])),
      },
    );
  }
}
