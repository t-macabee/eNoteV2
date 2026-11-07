import 'package:enote_core/enote_core.dart';

class ShopManufacturerProvider extends ReadOnlyProvider<ManufacturerDto> {
  ShopManufacturerProvider({required super.apiClient})
    : super(endpoint: 'shop/manufacturers');

  @override
  ManufacturerDto fromJson(Map<String, dynamic> json) =>
      ManufacturerDto.fromJson(json);
}
