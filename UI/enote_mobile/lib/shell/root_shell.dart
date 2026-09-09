import 'package:flutter/material.dart';

class RootShell extends StatelessWidget {
  const RootShell({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('eNote')),
      body: const Center(child: Text('Dobrodošli')),
    );
  }
}
