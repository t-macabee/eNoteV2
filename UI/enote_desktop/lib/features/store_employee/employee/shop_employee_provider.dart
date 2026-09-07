import 'package:enote_core/enote_core.dart';

class ShopEmployeeProvider extends BaseProvider<ShopEmployeeDto> {
  ShopEmployeeProvider({required super.apiClient})
      : super(endpoint: 'shop/employees');

  @override
  ShopEmployeeDto fromJson(Map<String, dynamic> json) =>
      ShopEmployeeDto.fromJson(json);

}
