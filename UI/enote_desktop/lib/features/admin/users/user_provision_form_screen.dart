import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/async_dropdown.dart';
import '../music_store/music_store_provider.dart';
import 'user_provision_service.dart';

/// Provisions a user via `POST admin/users`. There is no list/search or
/// activate/deactivate endpoint on `AdminUsersController`, so this form is
/// the only Users screen — always create mode (re-provisioning an existing
/// username updates its profile/role server-side instead of failing).
class UserProvisionFormScreen extends StatefulWidget {
  const UserProvisionFormScreen({super.key});

  @override
  State<UserProvisionFormScreen> createState() =>
      _UserProvisionFormScreenState();
}

class _UserProvisionFormScreenState extends State<UserProvisionFormScreen> {
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();

  UserRole? _role;
  int? _musicStoreId;

  @override
  void dispose() {
    _usernameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final service = context.read<UserProvisionService>();
    final request = UserProvisionRequest(
      username: _usernameController.text.trim(),
      email: _emailController.text.trim(),
      password: _passwordController.text,
      role: _role!.label,
      firstName: _firstNameController.text.trim(),
      lastName: _lastNameController.text.trim(),
      musicStoreId: _role == UserRole.storeEmployee ? _musicStoreId : null,
    );

    await service.provision(request);
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Kreiraj korisnika',
      fieldsBuilder: (context) => [
        TextFormField(
          controller: _usernameController,
          decoration: const InputDecoration(labelText: 'Korisničko ime'),
          validator: Validators.required('Korisničko ime'),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _emailController,
          decoration: const InputDecoration(labelText: 'Email'),
          keyboardType: TextInputType.emailAddress,
          validator: Validators.email,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _passwordController,
          decoration: const InputDecoration(labelText: 'Lozinka'),
          obscureText: true,
          validator: Validators.password,
        ),
        const SizedBox(height: 16),
        DropdownButtonFormField<UserRole>(
          initialValue: _role,
          decoration: const InputDecoration(labelText: 'Uloga'),
          items: UserRole.values
              .map((role) => DropdownMenuItem(
                    value: role,
                    child: Text(role.label),
                  ))
              .toList(),
          onChanged: (role) => setState(() => _role = role),
          validator: (value) =>
              value == null ? 'Uloga je obavezna.' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _firstNameController,
          decoration: const InputDecoration(labelText: 'Ime'),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime'),
        ),
        if (_role == UserRole.storeEmployee) ...[
          const SizedBox(height: 16),
          AsyncDropdown<MusicStoreDto>(
            label: 'Prodavnica',
            fetcher: () async {
              final result = await context.read<MusicStoreProvider>().search({
                'page': 1,
                'pageSize': 100,
                'includeTotalCount': false,
              });
              return result.items;
            },
            itemLabel: (store) => store.storeName,
            itemId: (store) => store.id,
            value: _musicStoreId,
            onChanged: (id, _) => setState(() => _musicStoreId = id as int?),
            validator: (value) => value == null
                ? 'Prodavnica je obavezna za StoreEmployee.'
                : null,
          ),
        ],
      ],
      onSave: _save,
      onReset: () {
        _usernameController.clear();
        _emailController.clear();
        _passwordController.clear();
        _firstNameController.clear();
        _lastNameController.clear();
        setState(() {
          _role = null;
          _musicStoreId = null;
        });
      },
      showCloseButton: false,
    );
  }
}
