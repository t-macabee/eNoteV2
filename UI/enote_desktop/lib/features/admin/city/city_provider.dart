import 'package:enote_core/enote_core.dart';

class CityProvider extends CrudProvider<CityDto> {
  CityProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/cities');

  @override
  CityDto fromJson(Map<String, dynamic> json) => CityDto.fromJson(json);
}
