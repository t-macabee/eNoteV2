import 'package:enote_core/enote_core.dart';

class InstrumentProvider extends BaseProvider<InstrumentDto> {
  InstrumentProvider({required super.apiClient})
      : super(endpoint: 'shop/instruments');

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);
}
