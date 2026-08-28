# eNote Flutter UI

Flutter client for the eNote platform: the shared `enote_core` package
(models, providers, HTTP client, auth) and the `enote_desktop` Windows app.

## Running the desktop app

From `UI/enote_desktop`:

```bash
flutter run --dart-define=API_BASE_URL=http://localhost:5059/api/v1/
```

`API_BASE_URL` is a build-time value (default: `http://localhost:5059/api/v1/`,
see `lib/config.dart`), so the app can point at any running API without a
source edit. Full default: `flutter run` is fine when the API runs on
`localhost:5059`.

## Tests

```bash
cd enote_core && flutter test
cd enote_desktop && flutter test
```
