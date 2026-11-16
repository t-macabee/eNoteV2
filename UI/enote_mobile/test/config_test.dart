import 'package:enote_mobile/config.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('validateStripePublishableKey throws on empty key', () {
    expect(
      () => validateStripePublishableKey(''),
      throwsA(
        isA<StateError>().having(
          (e) => e.message,
          'message',
          contains('STRIPE_PUBLISHABLE_KEY'),
        ),
      ),
    );
  });

  test('validateStripePublishableKey throws on README example key', () {
    expect(
      () => validateStripePublishableKey('pk_test_vas_kljuc'),
      throwsA(
        isA<StateError>().having(
          (e) => e.message,
          'message',
          contains('STRIPE_PUBLISHABLE_KEY'),
        ),
      ),
    );
  });

  test('validateStripePublishableKey accepts a test key', () {
    expect(() => validateStripePublishableKey('pk_test_51AbcDef123'), returnsNormally);
  });
}
