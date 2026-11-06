import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

class RoleBlockedScreen extends StatelessWidget {
  const RoleBlockedScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.block_outlined, size: AppSize.heroIcon),
              const SizedBox(height: AppSpacing.lg),
              const Text(
                'Ova aplikacija je namijenjena administratorima, instruktorima i '
                'uposlenicima prodavnice. Prijavite se na mobilnu aplikaciju.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.xl),
              FilledButton(
                onPressed: () =>
                    context.read<AuthState>().logout(revoke: true),
                child: const Text('Odjavi se'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
