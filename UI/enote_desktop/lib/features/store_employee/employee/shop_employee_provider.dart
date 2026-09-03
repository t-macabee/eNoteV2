import 'package:enote_core/enote_core.dart';

class ShopEmployeeProvider extends BaseProvider<ShopEmployeeDto> {
  ShopEmployeeProvider({required super.apiClient})
      : super(endpoint: 'shop/employees');

  @override
  ShopEmployeeDto fromJson(Map<String, dynamic> json) =>
      ShopEmployeeDto.fromJson(json);

  Future<int> createEmployee(DelegatedUserCreateRequest request) async {
    final response = await apiClient.post(
      'shop/employees',
      body: request.toJson(),
    );
    final data = response as Map<String, dynamic>;
    return data['userId'] as int? ?? 0;
  }
}
