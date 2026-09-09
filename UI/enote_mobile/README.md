# enote_mobile

Flutter client for eNote students. Shares models, HTTP client and auth with the
desktop app through the `enote_core` package.

## Running

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_your_key
```

`API_BASE_URL` defaults to `http://10.0.2.2:5059/api/v1/`, which is how the
Android emulator reaches the host machine. Both values are read with
`String.fromEnvironment` in `lib/config.dart`, so neither is hardcoded.

Sign in with `mobile` / `test` against the seeded API.

## Release build

```bash
flutter clean
flutter build apk --release
```

Output lands in `build/app/outputs/flutter-apk/app-release.apk`.

## Tests

```bash
flutter test
```
