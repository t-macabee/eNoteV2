import 'package:enote_core/enote_core.dart';

class StoreEmployeeProvider extends BaseProvider<ShopEmployeeDto> {
  StoreEmployeeProvider({
    required super.apiClient,
    super.endpoint = 'admin/employees',
  });

  @override
  ShopEmployeeDto fromJson(Map<String, dynamic> json) =>
      ShopEmployeeDto.fromJson(json);
}
