import 'package:enote_core/enote_core.dart';

class ShopAddressProvider extends ReadOnlyProvider<AddressReferenceDto> {
  ShopAddressProvider({required super.apiClient})
      : super(endpoint: 'shop/addresses');

  @override
  AddressReferenceDto fromJson(Map<String, dynamic> json) =>
      AddressReferenceDto.fromJson(json);
}
