import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  bool _busy = false;
  String? _message;

  Future<void> _ping() async {
    setState(() {
      _busy = true;
      _message = null;
    });
    try {
      final response = await context.read<ApiClient>().get('users/me');
      throwIfError(response);
      if (mounted) {
        setState(() => _message = 'OK');
      }
    } catch (e) {
      if (mounted) {
        setState(() => _message = userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Prijava')),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            FilledButton(
              onPressed: _busy ? null : _ping,
              child: Text(_busy ? '...' : 'Ping'),
            ),
            if (_message != null) ...[
              const SizedBox(height: 16),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Text(_message!, textAlign: TextAlign.center),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
