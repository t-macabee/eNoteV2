import 'package:enote_core/enote_core.dart';

class ShopInstrumentTypeProvider extends BaseProvider<InstrumentTypeDto> {
  ShopInstrumentTypeProvider({required super.apiClient})
      : super(endpoint: 'shop/instrument-types');

  @override
  InstrumentTypeDto fromJson(Map<String, dynamic> json) =>
      InstrumentTypeDto.fromJson(json);
}
