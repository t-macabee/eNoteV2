import 'package:enote_core/enote_core.dart';

/// Read-only address lookup for store employees (used by the "Uredi
/// prodavnicu" address dropdown) — mirrors [ShopInstrumentTypeProvider],
/// since address CRUD itself stays owned by admin/addresses.
class ShopAddressProvider extends BaseProvider<AddressReferenceDto> {
  ShopAddressProvider({required super.apiClient})
      : super(endpoint: 'shop/addresses');

  @override
  AddressReferenceDto fromJson(Map<String, dynamic> json) =>
      AddressReferenceDto.fromJson(json);
}
