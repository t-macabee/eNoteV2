import 'package:flutter/material.dart';
import 'package:provider/provider.dart';


import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/user_credential_fields.dart';
import 'shop_employee_provider.dart';

class ShopEmployeeFormScreen extends StatefulWidget {
  final EntityFormPresentation presentation;

  const ShopEmployeeFormScreen({
    super.key,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<ShopEmployeeFormScreen> createState() => _ShopEmployeeFormScreenState();
}

class _ShopEmployeeFormScreenState extends State<ShopEmployeeFormScreen> {
  final _credentialController = UserCredentialFieldsController();

  @override
  void dispose() {
    _credentialController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<ShopEmployeeProvider>();
    await provider.createDelegatedUser(_credentialController.buildRequest());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      presentation: widget.presentation,
      title: 'Kreiraj zaposlenika',
      saveLabel: 'Kreiraj',
      onSave: _save,
      fieldsBuilder: (context) => [
        UserCredentialFields(controller: _credentialController),
      ],
    );
  }
}
