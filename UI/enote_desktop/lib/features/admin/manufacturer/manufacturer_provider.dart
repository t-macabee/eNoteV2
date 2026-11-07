import 'package:enote_core/enote_core.dart';

class ManufacturerProvider extends CrudProvider<ManufacturerDto> {
  ManufacturerProvider({required super.apiClient})
    : super(endpoint: 'admin/manufacturers');

  @override
  ManufacturerDto fromJson(Map<String, dynamic> json) =>
      ManufacturerDto.fromJson(json);
}
