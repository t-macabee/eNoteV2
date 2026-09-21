import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/user_credential_fields.dart';
import '../music_store/music_store_provider.dart';
import 'admin_user_provider.dart';

/// Provisions a user via POST admin/users; UserGridScreen is the list/search/deactivate screen.
class UserProvisionFormScreen extends StatefulWidget {
  final EntityFormPresentation presentation;

  const UserProvisionFormScreen({
    super.key,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<UserProvisionFormScreen> createState() =>
      _UserProvisionFormScreenState();
}

class _UserProvisionFormScreenState extends State<UserProvisionFormScreen> {
  final _credentialController = UserCredentialFieldsController();

  UserRole? _role;
  int? _musicStoreId;
  bool _isManager = false;

  @override
  void dispose() {
    _credentialController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final service = context.read<AdminUserProvider>();
    final firstName = _credentialController.firstNameController.text.trim();
    final lastName = _credentialController.lastNameController.text.trim();
    final request = UserProvisionRequest(
      username: _credentialController.usernameController.text.trim(),
      email: _credentialController.emailController.text.trim(),
      password: _credentialController.passwordController.text,
      role: _role!.label,
      firstName: firstName.isEmpty ? null : firstName,
      lastName: lastName.isEmpty ? null : lastName,
      musicStoreId: _role == UserRole.storeEmployee ? _musicStoreId : null,
      isManager: _role == UserRole.storeEmployee ? _isManager : null,
    );

    await service.provision(request);
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Kreiraj korisnika',
      presentation: widget.presentation,
      fieldsBuilder: (context) => [
        UserCredentialFields(controller: _credentialController),
        DropdownButtonFormField<UserRole>(
          initialValue: _role,
          decoration: const InputDecoration(labelText: 'Uloga'),
          items: UserRole.values
              .map((role) => DropdownMenuItem(
                    value: role,
                    child: Text(role.label),
                  ))
              .toList(),
          onChanged: (role) => setState(() {
            _role = role;
            if (role != UserRole.storeEmployee) {
              _musicStoreId = null;
              _isManager = false;
            }
          }),
          validator: (value) =>
              value == null ? 'Uloga je obavezna.' : null,
        ),
        if (_role == UserRole.storeEmployee) ...[
          AsyncDropdown<MusicStoreDto>(
            label: 'Prodavnica',
            fetcher: () async {
              final result = await context.read<MusicStoreProvider>().search({
                'page': 1,
                'pageSize': 100,
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
          CheckboxListTile(
            title: const Text('Menadžer'),
            value: _isManager,
            onChanged: (value) => setState(() => _isManager = value ?? false),
            controlAffinity: ListTileControlAffinity.leading,
            contentPadding: EdgeInsets.zero,
          ),
        ],
      ],
      onSave: _save,
      onReset: () {
        _credentialController.clear();
        setState(() {
          _role = null;
          _musicStoreId = null;
          _isManager = false;
        });
      },
      showCloseButton: false,
    );
  }
}
