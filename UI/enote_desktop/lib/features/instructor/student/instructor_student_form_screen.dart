import 'package:flutter/material.dart';
import 'package:provider/provider.dart';


import '../../../widgets/entity_form_scaffold.dart';
import '../../../widgets/user_credential_fields.dart';
import 'instructor_student_provider.dart';

class InstructorStudentFormScreen extends StatefulWidget {
  final EntityFormPresentation presentation;

  const InstructorStudentFormScreen({
    super.key,
    this.presentation = EntityFormPresentation.page,
  });

  @override
  State<InstructorStudentFormScreen> createState() =>
      _InstructorStudentFormScreenState();
}

class _InstructorStudentFormScreenState
    extends State<InstructorStudentFormScreen> {
  final _credentialController = UserCredentialFieldsController();

  @override
  void dispose() {
    _credentialController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<InstructorStudentProvider>();
    await provider.createDelegatedUser(_credentialController.buildRequest());
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: 'Kreiraj studenta',
      saveLabel: 'Kreiraj',
      presentation: widget.presentation,
      onSave: _save,
      fieldsBuilder: (context) => [
        UserCredentialFields(controller: _credentialController),
      ],
    );
  }
}
