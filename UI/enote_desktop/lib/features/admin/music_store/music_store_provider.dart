import 'package:enote_core/enote_core.dart';

class MusicStoreProvider extends CrudProvider<MusicStoreDto> {
  MusicStoreProvider({
    required super.apiClient,
  }) : super(endpoint: 'admin/music-stores');

  @override
  MusicStoreDto fromJson(Map<String, dynamic> json) =>
      MusicStoreDto.fromJson(json);
}
