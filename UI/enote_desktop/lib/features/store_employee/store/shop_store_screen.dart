import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:enote_core/enote_core.dart';
import '../../../theme/app_theme.dart';
import '../../../widgets/async_dropdown.dart';
import '../../../widgets/image_upload_helper.dart';
import '../../admin/address/address_provider.dart';
import 'shop_store_provider.dart';

class ShopStoreScreen extends StatefulWidget {
  final MusicStoreDto? initialStore;

  const ShopStoreScreen({
    super.key,
    this.initialStore,
  });

  @override
  State<ShopStoreScreen> createState() => _ShopStoreScreenState();
}

class _ShopStoreScreenState extends State<ShopStoreScreen> {
  final _formKey = GlobalKey<FormState>();
  final _storeNameController = TextEditingController();
  final _businessHoursController = TextEditingController();
  final _phoneNumberController = TextEditingController();

  MusicStoreDto? _store;
  int? _selectedAddressId;
  String? _currentImagePath;
  bool _isLoading = true;
  String? _errorMessage;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    if (widget.initialStore != null) {
      _store = widget.initialStore;
      _populateFields(widget.initialStore!);
      _isLoading = false;
    } else {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _loadStore();
      });
    }
  }

  @override
  void dispose() {
    _storeNameController.dispose();
    _businessHoursController.dispose();
    _phoneNumberController.dispose();
    super.dispose();
  }

  void _populateFields(MusicStoreDto store) {
    _storeNameController.text = store.storeName;
    _businessHoursController.text = store.businessHours;
    _phoneNumberController.text = store.phoneNumber ?? '';
    _selectedAddressId = store.addressId;
    _currentImagePath = store.imagePath;
  }

  Future<void> _loadStore() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final store = await context.read<ShopStoreProvider>().getOwnStore();
      if (mounted) {
        setState(() {
          _store = store;
          _populateFields(store);
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = userMessage(e);
          _isLoading = false;
        });
      }
    }
  }

  Future<String?> _uploadImage(
      Uint8List bytes, String fileName, String contentType) async {
    final provider = context.read<ShopStoreProvider>();
    try {
      final updated =
          await provider.uploadOwnStoreImage(bytes, fileName, contentType);
      if (mounted) {
        setState(() {
          _currentImagePath = updated.imagePath;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Slika uspješno postavljena.')),
        );
      }
      return updated.imagePath;
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
      return null;
    }
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _isSaving = true);
    try {
      final provider = context.read<ShopStoreProvider>();
      final phone = _phoneNumberController.text.trim();
      final request = MusicStoreRequest(
        storeName: _storeNameController.text.trim(),
        businessHours: _businessHoursController.text.trim(),
        phoneNumber: phone.isEmpty ? null : phone,
        addressId: _selectedAddressId,
      );

      final updated = await provider.updateOwnStore(request.toJson());
      if (mounted) {
        setState(() {
          _store = updated;
          _currentImagePath = updated.imagePath;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Uspješno sačuvano.')),
        );
      }
    } catch (e) {
      if (mounted) {
        ErrorBanner.show(context, message: userMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isManager = context.watch<AuthState>().isManager;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Moja prodavnica'),
        automaticallyImplyLeading: false,
      ),
      body: _buildBody(context, isManager),
    );
  }

  Widget _buildBody(BuildContext context, bool isManager) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(_errorMessage!,
                style: const TextStyle(color: AppTheme.error)),
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: _loadStore,
              icon: const Icon(Icons.refresh),
              label: const Text('Pokušaj ponovo'),
            ),
          ],
        ),
      );
    }

    if (_store == null) {
      return const Center(child: Text('Nema podataka o prodavnici.'));
    }

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          TextFormField(
            controller: _storeNameController,
            readOnly: !isManager,
            decoration: const InputDecoration(labelText: 'Naziv'),
            validator: isManager ? Validators.required('Naziv') : null,
          ),
          const SizedBox(height: 18),
          TextFormField(
            controller: _businessHoursController,
            readOnly: !isManager,
            decoration: const InputDecoration(labelText: 'Radno vrijeme'),
            validator: isManager ? Validators.required('Radno vrijeme') : null,
          ),
          const SizedBox(height: 18),
          TextFormField(
            controller: _phoneNumberController,
            readOnly: !isManager,
            decoration: const InputDecoration(labelText: 'Broj telefona'),
          ),
          const SizedBox(height: 18),
          AsyncDropdown<AddressReferenceDto>(
            label: 'Adresa',
            value: _selectedAddressId,
            enabled: isManager,
            fetcher: () async {
              final provider = context.read<AddressProvider>();
              final result = await provider.search({
                'page': 1,
                'pageSize': 100,
              });
              return result.items;
            },
            itemLabel: (item) => '${item.street} ${item.number}, ${item.city}',
            itemId: (item) => item.id,
            onChanged: (id, item) {
              setState(() {
                _selectedAddressId = id as int?;
              });
            },
          ),
          const SizedBox(height: 24),
          const Text('Slika', style: TextStyle(fontWeight: FontWeight.bold)),
          if (isManager) ...[
            const SizedBox(height: 4),
            const Text(
              'Slika se automatski sprema prilikom odabira.',
              style: TextStyle(
                fontSize: 12,
                color: AppTheme.textSecondary,
              ),
            ),
          ],
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerLeft,
            child: ImageField(
              imageUrl: _currentImagePath,
              imagePicker: pickImageBytes,
              onUpload: isManager ? _uploadImage : null,
              editable: isManager,
              apiClient: context.read<ApiClient>(),
            ),
          ),
          if (isManager) ...[
            const SizedBox(height: 24),
            Align(
              alignment: Alignment.centerLeft,
              child: FilledButton.icon(
                onPressed: _isSaving ? null : _save,
                icon: _isSaving
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.save),
                label: const Text('Sačuvaj'),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
