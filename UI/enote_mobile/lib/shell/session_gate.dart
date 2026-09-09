import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../features/auth/login_screen.dart';
import 'role_blocked_screen.dart';
import 'root_shell.dart';

class SessionGate extends StatelessWidget {
  const SessionGate({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthState>(
      builder: (context, auth, _) {
        if (!auth.isAuthenticated) {
          return const LoginScreen();
        }
        if (!auth.hasRole('Student')) {
          return const RoleBlockedScreen();
        }
        return const RootShell();
      },
    );
  }
}
