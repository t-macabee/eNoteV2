import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../session/session_controller.dart';

class RoleBlockedScreen extends StatelessWidget {
  const RoleBlockedScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.block_outlined, size: 64),
              const SizedBox(height: 16),
              const Text(
                'Ova aplikacija je namijenjena studentima. Prijavite se na desktop aplikaciju.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: () =>
                    context.read<SessionController>().logoutAndRevoke(),
                child: const Text('Odjavi se'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
