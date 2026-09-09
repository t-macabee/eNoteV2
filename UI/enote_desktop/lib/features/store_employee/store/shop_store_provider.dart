import 'package:flutter/foundation.dart';

import 'package:enote_core/enote_core.dart';

/// Singleton store provider for `shop/store` (own store only).
///
/// Deliberately does NOT extend [BaseProvider]/[CrudProvider]/[ReadOnlyProvider]:
/// `shop/store` is a single-record resource with no id-taking
/// list/search/insert/update/remove shape. Previously it inherited those
/// methods and overrode `update(id,…)`/`uploadImage(id,…)` to discard `id` —
/// a Liskov violation (T9). The id-discarding overrides are deleted; callers
/// must use [getOwnStore]/[updateOwnStore]/[uploadOwnStoreImage] directly.
/// Verified: no screen calls search/getById/insert/remove on this provider;
/// `shop_store_provider_test` exercises only getOwnStore/updateOwnStore.
class ShopStoreProvider with ChangeNotifier {
  final ApiClient apiClient;
  final String endpoint = 'shop/store';

  ShopStoreProvider({required this.apiClient});

  MusicStoreDto? _store;
  MusicStoreDto? get store => _store;

  MusicStoreDto fromJson(Map<String, dynamic> json) =>
      MusicStoreDto.fromJson(json);

  Future<MusicStoreDto> getOwnStore() async {
    final response = await apiClient.get(endpoint);
    final data = decodeOrThrow(response);
    _store = fromJson(data);
    notifyListeners();
    return _store!;
  }

  Future<MusicStoreDto> getOne() => getOwnStore();

  Future<MusicStoreDto> updateOwnStore(Map<String, dynamic> request) async {
    final response = await apiClient.put(endpoint, body: request);
    final data = decodeOrThrow(response);
    _store = fromJson(data);
    notifyListeners();
    return _store!;
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
    _store = fromJson(data);
    notifyListeners();
    return _store!;
  }
}
