import 'package:enote_core/enote_core.dart';

class ShopEmployeeProvider extends BaseProvider<ShopEmployeeDto> {
  ShopEmployeeProvider({required super.apiClient})
      : super(endpoint: 'shop/employees');

  @override
  ShopEmployeeDto fromJson(Map<String, dynamic> json) =>
      ShopEmployeeDto.fromJson(json);

  Future<void> setActive(int userId, bool isActive) async {
    final response = await apiClient.put(
      '$endpoint/$userId/status',
      body: {'isActive': isActive},
    );
    throwIfError(response);
    notifyListeners();
  }
}
