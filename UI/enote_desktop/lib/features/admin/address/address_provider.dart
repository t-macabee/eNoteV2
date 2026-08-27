import 'package:enote_core/enote_core.dart';

class AddressProvider extends BaseProvider<AddressReferenceDto> {
  AddressProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/addresses');

  @override
  AddressReferenceDto fromJson(Map<String, dynamic> json) =>
      AddressReferenceDto.fromJson(json);
}
