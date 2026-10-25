import 'package:flutter_test/flutter_test.dart';
import 'package:enote_core/enote_core.dart';

void main() {
  group('EventDto imagePath', () {
    test('fromJson parses the server-computed default image', () {
      final dto = EventDto.fromJson({
        'id': 1,
        'title': 't',
        'description': 'd',
        'startsAt': '2026-09-10T18:00:00Z',
        'imagePath': '/api/v1/uploads/events/default.webp',
      });
      expect(dto.imagePath, '/api/v1/uploads/events/default.webp');
    });
  });

  group('EventRequest', () {
    test('toJson sends the platform-wide fields only', () {
      final json = EventRequest(
        title: 't',
        description: 'd',
        startsAt: DateTime.utc(2026, 9, 10, 18),
        addressId: 4,
      ).toJson();

      expect(json.keys, containsAll(['title', 'description', 'startsAt']));
      expect(json['addressId'], 4);
      expect(json.containsKey('courseId'), isFalse);
      expect(json.containsKey('instructorId'), isFalse);
    });
  });
}
