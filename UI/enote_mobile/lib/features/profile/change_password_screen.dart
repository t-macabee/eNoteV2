import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';

import '../../widgets/mobile_form_scaffold.dart';
import 'profile_provider.dart';

/// S32 — the old-password gate, reached only from the explicit
/// *Promijeni lozinku* action on S30. The session continues after a change
/// (no token revocation).
class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentController = TextEditingController();
  final _newController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _currentObscured = true;
  bool _newObscured = true;
  bool _confirmObscured = true;
  bool _busy = false;
  bool _canSubmit = false;
  String? _errorMessage;

  @override
  void dispose() {
    _currentController.dispose();
    _newController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  void _revalidate() {
    final form = _formKey.currentState;
    if (form == null) return;
    setState(() => _canSubmit = form.validate());
  }

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
    final provider = context.read<ProfileProvider>();
    setState(() {
      _busy = true;
      _errorMessage = null;
    });
    try {
      final newPassword = _newController.text;
      await provider.changePassword(
        ChangePasswordRequest(
          currentPassword: _currentController.text,
          newPassword: newPassword,
          confirmNewPassword: newPassword,
        ),
      );
      if (!mounted) return;
      messenger.showSnackBar(
        const SnackBar(content: Text('Lozinka je uspješno promijenjena.')),
      );
      Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        setState(() => _errorMessage = userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  InputDecoration _decoration(String label, bool obscured, VoidCallback toggle) {
    return InputDecoration(
      labelText: label,
      suffixIcon: IconButton(
        icon: Icon(obscured ? Icons.visibility : Icons.visibility_off),
        onPressed: toggle,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return MobileFormScaffold(
      title: 'Promjena lozinke',
      submitLabel: 'Promijeni lozinku',
      onSubmit: _canSubmit ? _submit : null,
      isBusy: _busy,
      errorMessage: _errorMessage,
      children: [
        Form(
          key: _formKey,
          autovalidateMode: AutovalidateMode.onUserInteraction,
          child: Column(
            children: [
              TextFormField(
                controller: _currentController,
                decoration: _decoration(
                  'Trenutna lozinka *',
                  _currentObscured,
                  () => setState(() => _currentObscured = !_currentObscured),
                ),
                obscureText: _currentObscured,
                textInputAction: TextInputAction.next,
                onChanged: (_) => _revalidate(),
                validator: Validators.required('Trenutna lozinka'),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _newController,
                decoration: _decoration(
                  'Nova lozinka *',
                  _newObscured,
                  () => setState(() => _newObscured = !_newObscured),
                ),
                obscureText: _newObscured,
                textInputAction: TextInputAction.next,
                onChanged: (_) => _revalidate(),
                validator: Validators.password,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _confirmController,
                decoration: _decoration(
                  'Potvrdi novu lozinku *',
                  _confirmObscured,
                  () =>
                      setState(() => _confirmObscured = !_confirmObscured),
                ),
                obscureText: _confirmObscured,
                textInputAction: TextInputAction.done,
                onChanged: (_) => _revalidate(),
                validator: (value) => Validators.confirmPassword(
                  _newController.text,
                )(value),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
