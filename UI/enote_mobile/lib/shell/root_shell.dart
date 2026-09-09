import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../session/session_controller.dart';
import 'app_router.dart';

class RootShell extends StatefulWidget {
  const RootShell({super.key});

  @override
  State<RootShell> createState() => _RootShellState();
}

class _RootShellState extends State<RootShell> {
  static const _titles = [
    'Instrumenti',
    'Učenje',
    'Iznajmljivanja',
    'Profil',
  ];

  int _index = 0;
  bool _booted = false;
  late final List<GlobalKey<NavigatorState>> _navKeys;

  @override
  void initState() {
    super.initState();
    _navKeys = List.generate(4, (_) => GlobalKey<NavigatorState>());
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!_booted) {
        _booted = true;
        if (mounted) {
          context.read<SessionController>().bootstrap();
        }
      }
    });
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
                onGenerateRoute: (_) => MaterialPageRoute(
                  builder: (_) => _TabRoot(index: i),
                ),
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
        title: Text(_RootShellState._titles[index]),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            onPressed: () => Navigator.of(
              context,
              rootNavigator: true,
            ).pushNamed(AppRouter.notifications),
          ),
        ],
      ),
      body: Center(child: Text(_RootShellState._titles[index])),
    );
  }
}
