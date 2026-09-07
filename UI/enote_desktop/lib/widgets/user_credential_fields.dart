import 'package:flutter/material.dart';
import 'package:enote_core/enote_core.dart';

class UserCredentialFieldsController {
  final usernameController = TextEditingController();
  final emailController = TextEditingController();
  final passwordController = TextEditingController();
  final firstNameController = TextEditingController();
  final lastNameController = TextEditingController();

  DelegatedUserCreateRequest buildRequest() {
    return DelegatedUserCreateRequest(
      username: usernameController.text.trim(),
      email: emailController.text.trim(),
      password: passwordController.text,
      firstName: firstNameController.text.trim().isEmpty
          ? null
          : firstNameController.text.trim(),
      lastName: lastNameController.text.trim().isEmpty
          ? null
          : lastNameController.text.trim(),
    );
  }

  void clear() {
    usernameController.clear();
    emailController.clear();
    passwordController.clear();
    firstNameController.clear();
    lastNameController.clear();
  }

  void dispose() {
    usernameController.dispose();
    emailController.dispose();
    passwordController.dispose();
    firstNameController.dispose();
    lastNameController.dispose();
  }
}

class UserCredentialFields extends StatelessWidget {
  final UserCredentialFieldsController controller;

  const UserCredentialFields({
    super.key,
    required this.controller,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextFormField(
          controller: controller.usernameController,
          decoration: const InputDecoration(labelText: 'Korisničko ime'),
          validator: Validators.required('Korisničko ime'),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: controller.emailController,
          decoration: const InputDecoration(labelText: 'Email'),
          keyboardType: TextInputType.emailAddress,
          validator: Validators.email,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: controller.passwordController,
          decoration: const InputDecoration(labelText: 'Lozinka'),
          obscureText: true,
          validator: Validators.password,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: controller.firstNameController,
          decoration: const InputDecoration(labelText: 'Ime'),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: controller.lastNameController,
          decoration: const InputDecoration(labelText: 'Prezime'),
        ),
      ],
    );
  }
}
