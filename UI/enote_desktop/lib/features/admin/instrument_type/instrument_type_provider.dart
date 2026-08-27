import 'package:enote_core/enote_core.dart';

class InstrumentTypeProvider extends BaseProvider<InstrumentTypeDto> {
  InstrumentTypeProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/instrument-types');

  @override
  InstrumentTypeDto fromJson(Map<String, dynamic> json) =>
      InstrumentTypeDto.fromJson(json);
}
