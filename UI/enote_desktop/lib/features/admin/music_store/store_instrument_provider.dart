import 'package:enote_core/enote_core.dart';

class StoreInstrumentProvider extends ReadOnlyProvider<InstrumentDto> {
  StoreInstrumentProvider({
    required super.apiClient,
  }) : super(endpoint: 'instruments/public');

  @override
  InstrumentDto fromJson(Map<String, dynamic> json) =>
      InstrumentDto.fromJson(json);
}
