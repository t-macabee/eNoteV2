import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import 'profile_provider.dart';

const _passwordErrorCode = 'error.current_password_invalid';

Future<bool?> showDeleteProfileDialog(BuildContext context) {
  return showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (context) => const DeleteProfileDialog(),
  );
}

class DeleteProfileDialog extends StatefulWidget {
  const DeleteProfileDialog({super.key});

  @override
  State<DeleteProfileDialog> createState() => _DeleteProfileDialogState();
}

class _DeleteProfileDialogState extends State<DeleteProfileDialog> {
  final _formKey = GlobalKey<FormState>();
  final _passwordController = TextEditingController();
  bool _confirmed = false;
  bool _isBusy = false;
  String? _passwordError;
  String? _error;

  @override
  void dispose() {
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() {
      _isBusy = true;
      _passwordError = null;
      _error = null;
    });
    try {
      await context.read<ProfileProvider>().deleteProfile(
        DeleteProfileRequest(currentPassword: _passwordController.text),
      );
      if (mounted) Navigator.of(context).pop(true);
      return;
    } catch (e) {
      if (!mounted) return;
      setState(() {
        if (e is ApiException && e.code == _passwordErrorCode) {
          _passwordError = e.message;
        } else {
          _error = userMessage(e);
        }
      });
    }
    if (mounted) setState(() => _isBusy = false);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Brisanje profila'),
      content: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text(
              'Brisanjem profila trajno se uklanjaju lični podaci: ime, email, '
              'datum rođenja, broj telefona i profilna slika. Račun se zatvara '
              'i sve sesije se odjavljuju.',
            ),
            const SizedBox(height: 12),
            const Text(
              'Evidencije o uplatama, iznajmljivanjima, ocjenama i prisustvu '
              'ostaju sačuvane. Zahtjevi za iznajmljivanje i upisi na kurs koji '
              'su na čekanju ili aktivni se otkazuju.',
            ),
            const SizedBox(height: 16),
            Form(
              key: _formKey,
              child: TextFormField(
                controller: _passwordController,
                obscureText: true,
                autofocus: true,
                decoration: InputDecoration(
                  labelText: 'Trenutna lozinka *',
                  errorText: _passwordError,
                ),
                validator: Validators.required('Trenutna lozinka'),
                onChanged: (_) {
                  if (_passwordError != null) {
                    setState(() => _passwordError = null);
                  }
                },
              ),
            ),
            const SizedBox(height: 8),
            CheckboxListTile(
              value: _confirmed,
              onChanged: _isBusy
                  ? null
                  : (value) => setState(() => _confirmed = value ?? false),
              controlAffinity: ListTileControlAffinity.leading,
              contentPadding: EdgeInsets.zero,
              title: const Text('Potvrđujem trajno brisanje profila'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 8),
              ErrorBanner(message: _error!),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isBusy ? null : () => Navigator.of(context).pop(false),
          child: const Text('Otkaži'),
        ),
        ElevatedButton(
          onPressed: (!_confirmed || _isBusy) ? null : _submit,
          child: _isBusy
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Text('Obriši profil'),
        ),
      ],
    );
  }
}
