import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'shop_employee_provider.dart';

class ShopEmployeeFormScreen extends StatefulWidget {
  const ShopEmployeeFormScreen({super.key});

  @override
  State<ShopEmployeeFormScreen> createState() => _ShopEmployeeFormScreenState();
}

class _ShopEmployeeFormScreenState extends State<ShopEmployeeFormScreen> {
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();

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
    final provider = context.read<ShopEmployeeProvider>();
    final request = DelegatedUserCreateRequest(
      username: _usernameController.text.trim(),
      email: _emailController.text.trim(),
      password: _passwordController.text,
      firstName: _firstNameController.text.trim().isEmpty
          ? null
          : _firstNameController.text.trim(),
      lastName: _lastNameController.text.trim().isEmpty
          ? null
          : _lastNameController.text.trim(),
    );

    await provider.createEmployee(request);
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Kreiraj zaposlenika',
      saveLabel: 'Kreiraj',
      onSave: _save,
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
        TextFormField(
          controller: _firstNameController,
          decoration: const InputDecoration(labelText: 'Ime'),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime'),
        ),
      ],
    );
  }
}
