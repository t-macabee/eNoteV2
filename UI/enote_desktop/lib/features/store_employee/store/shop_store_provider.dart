import 'package:enote_core/enote_core.dart';

class ShopStoreProvider extends BaseProvider<MusicStoreDto> {
  ShopStoreProvider({required super.apiClient}) : super(endpoint: 'shop/store');

  @override
  MusicStoreDto fromJson(Map<String, dynamic> json) =>
      MusicStoreDto.fromJson(json);

  Future<MusicStoreDto> getOwnStore() async {
    final response = await apiClient.get(endpoint);
    final data = decodeOrThrow(response);
    return fromJson(data);
  }

  Future<MusicStoreDto> getOne() => getOwnStore();

  Future<MusicStoreDto> updateOwnStore(Map<String, dynamic> request) async {
    final response = await apiClient.put(endpoint, body: request);
    final data = decodeOrThrow(response);
    notifyListeners();
    return fromJson(data);
  }

  Future<MusicStoreDto> uploadOwnStoreImage(
    List<int> bytes,
    String fileName,
    String contentType,
  ) async {
    final response = await apiClient.postMultipart(
      '$endpoint/image',
      bytes: bytes,
      fileName: fileName,
      contentType: contentType,
    );
    final data = decodeOrThrow(response);
    final updated = fromJson(data);
    notifyListeners();
    return updated;
  }

  @override
  Future<MusicStoreDto> update(int id, Map<String, dynamic> request) =>
      updateOwnStore(request);

  @override
  Future<MusicStoreDto> uploadImage(
    int id,
    List<int> bytes,
    String fileName,
    String contentType,
  ) =>
      uploadOwnStoreImage(bytes, fileName, contentType);
}
