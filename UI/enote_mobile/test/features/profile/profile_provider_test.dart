import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;

import 'package:enote_core/enote_core.dart';
import 'package:enote_mobile/features/profile/profile_provider.dart';

import '../../helpers.dart';

const _meBody = {
  'role': 'Student',
  'username': 'student',
  'email': 'student@enote.com',
  'profile': {
    'id': 7,
    'firstName': 'Student',
    'lastName': 'Enote',
    'dateOfBirth': '2001-05-12T00:00:00',
  },
  'hasPicture': false,
};

class _MultipartStubClient extends http.BaseClient {
  final List<http.BaseRequest> sent = [];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    sent.add(request);
    return http.StreamedResponse(
      Stream.value(utf8.encode('{}')),
      200,
      headers: {'content-type': 'application/json'},
    );
  }
}

ProfileProvider _provider(http.Client client) {
  final authState = AuthState(
    baseUrl: 'http://10.0.2.2:5059/api/v1/',
    tokenReader: () => fakeJwt(),
    httpClient: client,
  );
  return ProfileProvider(
    apiClient: ApiClient(
      baseUrl: 'http://10.0.2.2:5059/api/v1/',
      authState: authState,
      httpClient: client,
    ),
  );
}

void main() {
  test('getMe issues GET users/me and decodes the profile', () async {
    final client = RecordingHttpClient(body: _meBody);
    final provider = _provider(client);
    final profile = await provider.getMe();

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'GET');
    expect(client.requests.single.url.path, '/api/v1/users/me');
    expect(profile.username, 'student');
    expect(profile.profile.firstName, 'Student');
    expect(profile.profile.dateOfBirth, DateTime(2001, 5, 12));
    expect(profile.hasPicture, isFalse);
  });

  test('updateMe issues PUT users/me with the UpdateProfileRequest body',
      () async {
    final client = RecordingHttpClient(body: _meBody);
    final provider = _provider(client);
    final request = UpdateProfileRequest(
      email: 'new@enote.com',
      firstName: 'Novo',
      lastName: 'Prezime',
      dateOfBirth: DateTime(2002, 3, 4),
    );
    await provider.updateMe(request);

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'PUT');
    expect(sent.url.path, '/api/v1/users/me');
    final body = jsonDecode(sent.body) as Map<String, dynamic>;
    expect(body, {
      'email': 'new@enote.com',
      'firstName': 'Novo',
      'lastName': 'Prezime',
      'dateOfBirth': '2002-03-04T00:00:00.000',
    });
  });

  test('updateMe omits null optional members from the JSON body', () async {
    final client = RecordingHttpClient(body: _meBody);
    final provider = _provider(client);
    await provider.updateMe(UpdateProfileRequest(email: 'e@enote.com'));

    final sent = client.requests.single;
    final body = jsonDecode(sent.body) as Map<String, dynamic>;
    expect(body, {'email': 'e@enote.com'});
  });

  test('uploadPicture issues PUT users/me/picture with the file part',
      () async {
    final client = _MultipartStubClient();
    final provider = _provider(client);
    final bytes = List<int>.generate(8, (i) => i);
    await provider.uploadPicture(bytes, 'slika.jpg', 'image/jpeg');

    expect(client.sent, hasLength(1));
    final request = client.sent.single;
    expect(request.method, 'PUT');
    expect(request.url.path, '/api/v1/users/me/picture');
    expect(request, isA<http.MultipartRequest>());
    final multipart = request as http.MultipartRequest;
    expect(multipart.files, hasLength(1));
    final part = multipart.files.single;
    expect(part.field, 'file');
    expect(part.filename, 'slika.jpg');
    expect(part.contentType.mimeType, 'image/jpeg');
    expect(await part.finalize().toBytes(), bytes);
  });

  test('deletePicture issues DELETE users/me/picture', () async {
    final client = RecordingHttpClient(body: const {});
    final provider = _provider(client);
    await provider.deletePicture();

    expect(client.requests, hasLength(1));
    expect(client.requests.single.method, 'DELETE');
    expect(client.requests.single.url.path, '/api/v1/users/me/picture');
  });

  test('changePassword issues PUT users/me/password with the request body',
      () async {
    final client = RecordingHttpClient(body: const {});
    final provider = _provider(client);
    await provider.changePassword(
      ChangePasswordRequest(
        currentPassword: 'Stara1!',
        newPassword: 'Nova2@Bcd',
        confirmNewPassword: 'Nova2@Bcd',
      ),
    );

    expect(client.requests, hasLength(1));
    final sent = client.requests.single;
    expect(sent.method, 'PUT');
    expect(sent.url.path, '/api/v1/users/me/password');
    final body = jsonDecode(sent.body) as Map<String, dynamic>;
    expect(body, {
      'currentPassword': 'Stara1!',
      'newPassword': 'Nova2@Bcd',
      'confirmNewPassword': 'Nova2@Bcd',
    });
  });
}
