import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:enote_core/enote_core.dart';
import '../../widgets/entity_form_scaffold.dart';

class ProfileDialog extends StatefulWidget {
  const ProfileDialog({super.key});

  @override
  State<ProfileDialog> createState() => _ProfileDialogState();
}

class _ProfileDialogState extends State<ProfileDialog> {
  UserProfileResponse? _profileResponse;
  bool _isLoading = true;
  bool _isSaving = false;

  final _emailController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _dateOfBirthController = TextEditingController();

  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();

  final _formKey = GlobalKey<FormState>();
  final _passwordFormKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.get('users/me');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        final profileResponse = UserProfileResponse.fromJson(data);
        if (mounted) {
          setState(() {
            _profileResponse = profileResponse;
            final p = profileResponse.profile;
            _emailController.text = profileResponse.email ?? '';
            _firstNameController.text = p.firstName ?? '';
            _lastNameController.text = p.lastName ?? '';
            _dateOfBirthController.text = p.dateOfBirth?.toIso8601String().split('T').first ?? '';
            _isLoading = false;
          });
        }
      } else {
        if (mounted) setState(() => _isLoading = false);
      }
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
        setState(() => _isLoading = false);
      }
    }
  }

  Future<bool> _updateProfile() async {
    if (!(_formKey.currentState?.validate() ?? false)) return false;

    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.put(
        'users/me',
        body: {
          'email': _emailController.text,
          'firstName': _firstNameController.text,
          'lastName': _lastNameController.text,
          'dateOfBirth': _dateOfBirthController.text.isNotEmpty ? _dateOfBirthController.text : null,
        },
      );
      if (response.statusCode >= 400) {
        throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
      }
      return true;
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
      return false;
    }
  }

  Future<bool> _changePassword() async {
    if (!(_passwordFormKey.currentState?.validate() ?? false)) return false;

    setState(() => _isSaving = true);
    try {
      final apiClient = context.read<ApiClient>();
      final response = await apiClient.put(
        'users/me/password',
        body: {
          'currentPassword': _currentPasswordController.text,
          'newPassword': _newPasswordController.text,
        },
      );
      if (response.statusCode >= 400) {
        throw ApiException(ApiErrorMapper.mapError(response.statusCode, response.body));
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Lozinka uspješno promijenjena.')),
        );
        _currentPasswordController.clear();
        _newPasswordController.clear();
      }
      return true;
    } catch (e) {
      if (mounted) ErrorBanner.show(context, message: userMessage(e));
      return false;
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Dialog(
        child: Padding(
          padding: EdgeInsets.all(40),
          child: CircularProgressIndicator(),
        ),
      );
    }

    if (_profileResponse == null) {
      return const Dialog(
        child: Padding(
          padding: EdgeInsets.all(40),
          child: Text('Greška pri učitavanju profila.'),
        ),
      );
    }

    final role = _profileResponse!.role;

    return EntityFormScaffold(
      title: 'Moj Profil',
      presentation: EntityFormPresentation.dialog,
      isEditMode: true,
      onSave: _updateProfile,
      fieldsBuilder: (context) => [
        Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Korisničko ime: ${_profileResponse!.username}', style: const TextStyle(fontWeight: FontWeight.bold)),
              Text('Uloga: $role', style: const TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 16),
              TextFormField(
                controller: _firstNameController,
                decoration: const InputDecoration(labelText: 'Ime'),
                validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _lastNameController,
                decoration: const InputDecoration(labelText: 'Prezime'),
                validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _emailController,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _dateOfBirthController,
                decoration: const InputDecoration(labelText: 'Datum rođenja (YYYY-MM-DD)'),
              ),
            ],
          ),
        ),
        const Divider(height: 48),
        Text('Promjena lozinke', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 16),
        Form(
          key: _passwordFormKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _currentPasswordController,
                decoration: const InputDecoration(labelText: 'Trenutna lozinka'),
                obscureText: true,
                validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _newPasswordController,
                decoration: const InputDecoration(labelText: 'Nova lozinka'),
                obscureText: true,
                validator: (v) => v?.isEmpty ?? true ? 'Obavezno polje' : null,
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _isSaving ? null : _changePassword,
                child: const Text('Promijeni lozinku'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
