const String kApiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://10.0.2.2:5059/api/v1/',
);
const String kStripePublishableKey = String.fromEnvironment(
  'STRIPE_PUBLISHABLE_KEY',
);

void validateStripePublishableKey(String key) {
  final trimmed = key.trim();
  if (trimmed.isEmpty ||
      trimmed == 'pk_test_vas_kljuc' ||
      trimmed == 'pk_test_...') {
    throw StateError(
      'STRIPE_PUBLISHABLE_KEY is missing or uses an example value. Pass a valid Stripe publishable key via --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_....',
    );
  }
}
