import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../widgets/entity_form_scaffold.dart';
import 'music_store_provider.dart';

class MusicStoreFormScreen extends StatefulWidget {
  final MusicStoreDto? existing;

  const MusicStoreFormScreen({super.key, this.existing});

  @override
  State<MusicStoreFormScreen> createState() => _MusicStoreFormScreenState();
}

class _MusicStoreFormScreenState extends State<MusicStoreFormScreen> {
  final _storeNameController = TextEditingController();
  final _businessHoursController = TextEditingController();

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _storeNameController.text = existing.storeName;
      _businessHoursController.text = existing.businessHours;
    }
  }

  @override
  void dispose() {
    _storeNameController.dispose();
    _businessHoursController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    final provider = context.read<MusicStoreProvider>();
    final request = MusicStoreRequest(
      storeName: _storeNameController.text.trim(),
      businessHours: _businessHoursController.text.trim(),
    );

    if (widget.existing == null) {
      await provider.insert(request.toJson());
    } else {
      await provider.update(widget.existing!.id, request.toJson());
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return EntityFormScaffold(
      title: widget.existing == null ? 'Dodaj prodavnicu' : 'Uredi prodavnicu',
      isEditMode: widget.existing != null,
      fieldsBuilder: (_) => [
        TextFormField(
          controller: _storeNameController,
          decoration: const InputDecoration(labelText: 'Naziv'),
          validator: Validators.required('Naziv'),
        ),
        TextFormField(
          controller: _businessHoursController,
          decoration: const InputDecoration(labelText: 'Radno vrijeme'),
          validator: Validators.required('Radno vrijeme'),
        ),
      ],
      onSave: _save,
    );
  }
}
