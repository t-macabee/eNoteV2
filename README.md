# eNote

**English summary:** eNote is a platform for a music school: courses, lectures, assignments, instrument rentals and notifications. It has an ASP.NET Core API (SQL Server, EF Core, JWT with four roles), a separate worker service on RabbitMQ for email and notifications, Stripe payments with signed webhooks, and two Flutter apps that share one Dart package: a Windows desktop app for staff and an Android app for students. 700+ backend tests run in CI. The rest of this README is in Bosnian, as the faculty requires.

Platforma za muzičku školu. Pokriva kurseve, predavanja, zadatke, najam
instrumenata i notifikacije. Sastoji se od ASP.NET Core API-ja, zasebnog worker
servisa i dva Flutter klijenta koji se nalaze u ovom repozitoriju pod `UI/`:
`enote_desktop` (administrator, instruktor, prodavac) i `enote_mobile`
(student). Zajednički Dart paket `UI/enote_core` oba klijenta referenciraju
preko putanje.

## Preduslovi

- [.NET 10 SDK](https://dotnet.microsoft.com/download), verzija je zaključana u `global.json`
- Docker Desktop, ako aplikaciju pokrećete kroz `docker compose`
- Flutter SDK, za desktop i mobilnu aplikaciju
- SQL Server i RabbitMQ, samo ako sve pokrećete lokalno bez Dockera

## Pokretanje kroz Docker

Ovo je najkraći put i pokreće bazu, RabbitMQ, API i worker odjednom.

1. Napravite `.env` u korijenu repozitorija.

   Za pregled rada: raspakujte `.env-tajne.zip` u korijen repozitorija.
   Šifra za arhivu je postavljena na DLWMS. Arhiva sadrži gotov `.env` sa
   svim vrijednostima, pa ništa ne treba popunjavati.

   Bez arhive, napravite `.env` iz primjera:

   ```bash
   cp .env.docker.example .env
   ```

   Svi ključevi iz primjera su obavezni, osim `Smtp__Username` i `Smtp__Password`,
   koji ostaju prazni za Mailhog, i zakomentarisanih vrijednosti na kraju datoteke.
   Zamijenite placeholder vrijednosti: `MSSQL_SA_PASSWORD`, `Jwt__Key` (najmanje
   32 znaka), `Stripe__SecretKey`, `Stripe__WebhookSecret` i
   `STRIPE_PUBLISHABLE_KEY`. Ostale vrijednosti iz primjera rade lokalno bez izmjene.
   Svaka vrijednost se upisuje samo jednom, u `.env`;
   `docker compose` prosljeđuje tu datoteku api i worker kontejnerima i
   zamjenjuje samo imena hostova (`sqlserver`, `rabbitmq`, `mailhog`).

2. Ako ste repozitorij već pokretali kroz Docker, uklonite stare volumene.
   Docker u imenovani volumen kopira slike iz seeda samo pri kreiranju
   volumena, a baza zadržava stare seed redove koji mogu pokazivati na
   obrisane datoteke:

   ```bash
   docker compose down -v
   ```

   Time se brišu `enote_enote-sql-data` i `enote_enote-uploads`. Baza se
   ponovo kreira i popunjava seed podacima pri sljedećem pokretanju.

3. Pokrenite sve servise:

   ```bash
   docker compose up --build
   ```

API sluša na `http://localhost:5059`. Migracije i seed podaci se primjenjuju
automatski pri prvom pokretanju u Development okruženju.

Za fizički telefon API mora slušati na svim interfejsima
(`dotnet run --project eNote.API --urls http://0.0.0.0:5059`) ili se mora
koristiti Docker, koji već objavljuje port na svim interfejsima.

## Pokretanje bez Dockera

`dotnet run` čita isti `.env`. Mail ide na Mailhog na `localhost:1025`; pokrenite ga sa `docker compose up -d mailhog`.

Iz foldera `eNote/`:

```bash
dotnet restore
dotnet ef database update --project eNote.Infrastructure --startup-project eNote.API
dotnet run --project eNote.API
```

Worker se pokreće posebno, u drugom terminalu:

```bash
dotnet run --project eNote.Worker
```

Worker je zaseban projekat i zaseban kontejner. API objavljuje notifikacijske
poruke na RabbitMQ i ponavlja neuspjela slanja. Worker ih preuzima sa RabbitMQ-a,
sprema notifikacije i šalje mail kada je zahtjev za najam odobren, odbijen ili
otkazan.

Worker svakih 5 minuta odbija odobrena iznajmljivanja koja nisu preuzeta u roku od 2 dana od odobrenja. Student dobija notifikaciju i mail s razlogom.

## Prijava

Svi seed nalozi koriste lozinku `test`. Lozinka se može promijeniti kroz
`Seed__DefaultPassword` u `.env`.

| Korisničko ime | Lozinka | Uloga | Gdje se koristi |
|---|---|---|---|
| desktop | test | Administrator | Desktop aplikacija |
| mobile | test | Student | Mobilna aplikacija |
| admin | test | Administrator | Desktop aplikacija |
| instructor | test | Instructor | Desktop aplikacija |
| student | test | Student | Mobilna aplikacija |
| storeemployee | test | StoreEmployee | Desktop aplikacija |
| student1 … student6 | test | Student | Mobilna aplikacija |

Svi nalozi dijele istu seed lozinku (`Seed__DefaultPassword`). Mobilna
aplikacija prihvata samo naloge sa ulogom Student (`student`, odnosno `mobile`);
ostale uloge dobijaju ekran sa objašnjenjem. Svi seedovani studenti su upisani
na prva dva objavljena kursa i imaju članarinu važeću godinu dana. Treći kurs
"Solfeđo za početnike" prima zahtjeve za upis: `student` ima zahtjev koji čeka
odobrenje, a `instructor` ga odobrava ili odbija na desktop aplikaciji
(Kursevi → kurs → Upisi); `student1` ima odbijen zahtjev s razlogom. `mobile`
ima jedno završeno iznajmljivanje koje nije plaćeno (prikazuje Stripe plaćanje,
a dok se ne plati `mobile` ne može zatražiti novo iznajmljivanje); taj najam je
vraćen 2 dana nakon roka i njegov iznos uključuje penal. `student` ima aktivno
iznajmljivanje unutar prozora za produženje, a `student1` aktivno iznajmljivanje
koje kasni. Seedovana plaćena iznajmljivanja nemaju Stripe plaćanje, pa se
povrat novca prikazuje uz novo testno plaćanje.

Registracija novih korisnika kroz aplikaciju i dalje traži jaču lozinku
(najmanje 8 znakova, veliko slovo, malo slovo, broj i specijalni znak). Seed
nalozi su izuzetak i postoje samo da pregled aplikacije bude brz.

## Desktop aplikacija

Iz foldera `UI/enote_desktop`:

```bash
flutter run --dart-define=API_BASE_URL=http://localhost:5059/api/v1/
```

`API_BASE_URL` je build-time vrijednost sa podrazumijevanim
`http://localhost:5059/api/v1/`, pa se adresa API-ja mijenja bez diranja koda.

## Mobilna aplikacija

Iz foldera `UI/enote_mobile`:

```bash
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_vas_kljuc
```

`10.0.2.2` je adresa preko koje Android emulator vidi host mašinu. Putanja mora
sadržavati `/api/v1/`. Stripe publishable ključ se prosljeđuje na isti način jer
ga API ne izlaže kroz endpoint.

### Fizički uređaj

Telefon i računar moraju biti na istoj mreži. API tada mora biti dostupan sa
mreže (vidi napomenu iznad za `--urls http://0.0.0.0:5059` ili Docker), a LAN
adresu računara (`ipconfig`) treba dodati u
`UI/enote_mobile/android/app/src/main/res/xml/network_security_config.xml`.
Zatim pokrenite, uz dozvoljen dolazni TCP 5059 kroz firewall:

```bash
flutter run -d <device-id> --dart-define=API_BASE_URL=http://192.168.1.20:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_vas_kljuc
```

Release APK za predaju (defines su ugrađene, tuđa mašina ne treba okruženje):

```bash
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5059/api/v1/ --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_vas_kljuc
```

Link za reset lozinke iz e-maila (Mailhog) se na emulatoru otvara kroz `adb`.
Token kopirajte iz linka u e-mailu:

```bash
adb shell am start -W -a android.intent.action.VIEW -d "enote://reset-password?email=student%40enote.com&token=<token>"
```

Master–details forma na mobilnom klijentu je detalj *Kurs*
(`CourseDetailScreen`): zaglavlje kursa i njegova paginirana predavanja; zahtjev
za upis čeka odobrenje instruktora, odobren upis prikazuje predavanja, a ispis
zadržava pristup do isteka plaćenog perioda.

Student na ekranu *Profil* briše svoj nalog kroz `DELETE /api/v1/users/me`.
Brisanje nije fizičko: uplate, iznajmljivanja, ocjene i prisustvo ostaju, a lični
podaci se anonimiziraju i sve sesije se prekidaju. Nalog se ne briše dok student
ima neplaćen završen najam, najam u toku ili uplatu koja se obrađuje.

## Plaćanja

Najam instrumenta traje 30 dana od preuzimanja (`DueAt = PickedUpAt + 30 dana`).

Prodavnica povrat prije isteka roka evidentira kao prijevremeni povrat (ReturnedEarly), a povrat na dan isteka roka ili kasnije kao završetak (Completed).

Student može produžiti aktivni najam u zadnjih 5 dana trenutnog roka; svako
produženje dodaje još 30 dana i ne naplaćuje se. Za svaki dan nakon isteka roka
obračunava se penal po danu: `(Fee / 30) × Rentals__OverduePenaltyMultiplier`.
Multiplikator se kopira na najam pri odobrenju, pa kasnija promjena u `.env` ne
mijenja postojeće najmove. Najam se naplaćuje jednom, kada pređe u status
`Complete` ili `ReturnedEarly`, a uplaćeni iznos uključuje penal. Iznos računa
server. Podrazumijevana valuta je BAM i mijenja se kroz `Stripe__Currency`;
`Rentals__OverduePenaltyMultiplier` (0.01–10) određuje penal.

Školarina se naplaćuje po kursu, mjesečno. `Course.Price` je mjesečna cijena
kursa; student je plaća unaprijed i time dobija 30 dana pristupa sadržaju tog
kursa. Obnova je ručna, a ponovna uplata prije isteka ne gubi dane
(`PaidUntil = max(trenutno, PaidUntil) + 30 dana`). Kurs sa cijenom 0 je
besplatan. Iznos i ovdje računa server.

Ispis sa kursa ne vraća uplaćenu školarinu. Pristup sadržaju ostaje do
`PaidUntil`, a ponovni upis prije isteka nastavlja pretplatu bez novog
odobrenja instruktora i bez nove uplate.

Ako uplata školarine stigne za upis koji više nije aktivan ili za objavu kursa
koja je uklonjena, webhook je označi kao uspješnu, ne daje pristup i odmah
pokreće povrat cijelog iznosa. Isto važi za drugu uplatu istog najma. Puni
povrat uplate koja je produžila pristup vraća `PaidUntil` na početak tog
perioda; djelimični povrat ne mijenja pristup.

Obavezne Stripe varijable su `Stripe__SecretKey`, `Stripe__WebhookSecret`,
`Stripe__Currency`, `Stripe__RentalStatementDescriptorSuffix` i
`Stripe__TuitionStatementDescriptorSuffix`. Tekst na izvodu kartice ima najviše
22 znaka, mora imati bar jedno slovo i koristi samo slova, cifre, razmak i
znakove `. , & / -`. Webhook
treba usmjeriti na `POST /api/v1/payments/stripe/webhook`, lokalno preko
`stripe listen`.

Mobilna aplikacija plaća unutar aplikacije kroz Stripe PaymentSheet. Publishable
ključ se ne izlaže kroz endpoint nego se prosljeđuje sa
`--dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_…`. Za lokalno testiranje pokrenite
`stripe listen --forward-to localhost:5059/api/v1/payments/stripe/webhook`; isti
webhook pokriva i najam i školarinu.

Za pregled rada treba [Stripe CLI](https://docs.stripe.com/stripe-cli). Prijava
na Stripe nalog nije potrebna. Kada API radi, u drugom terminalu pokrenite:

```bash
stripe listen --api-key <Stripe__SecretKey iz .env> --forward-to localhost:5059/api/v1/payments/stripe/webhook
```

Bez ovog koraka webhook ne stiže do API-ja i plaćanje ostaje neplaćeno.

| Endpoint | Uloga |
|---|---|
| `POST /api/v1/student/rentals/{id}/renew` | Student |
| `POST /api/v1/student/rentals/{rentalId}/payments/create-intent` | Student |
| `GET /api/v1/student/rentals/{rentalId}/payments` | Student |
| `POST /api/v1/shop/rentals/{rentalId}/payments/refund` | StoreEmployee |
| `POST /api/v1/student/enrollments/{enrollmentId}/tuition/create-intent` | Student |
| `GET /api/v1/student/enrollments/{enrollmentId}/tuition` | Student |
| `GET /api/v1/student/enrollments/{enrollmentId}/tuition/history` | Student |
| `GET /api/v1/instructor/students/{id}/enrollments` | Instructor |
| `GET /api/v1/instructor/student-directory` | Instructor |
| `GET /api/v1/instructor/student-directory/{id}` | Instructor |
| `POST /api/v1/payments/stripe/webhook` | Stripe, potpis se provjerava |

Povrat novca ne poništava status plaćenog najma. Student koji ima neplaćen
završen najam ne može zatražiti novi dok ga ne plati.

## PDF izvještaji

| Endpoint | Uloga |
|---|---|
| `GET /api/v1/admin/music-stores/report` | Administrator |
| `GET /api/v1/instructor/courses/{courseId}/ranking/report` | Instructor |
| `GET /api/v1/instructor/lectures/{id}/attendance/report` | Instructor |
| `GET /api/v1/shop/rentals/report` | StoreEmployee |

## Testovi

```bash
dotnet test
```

`dotnet test` za testove u `eNote.Tests/Integration` zahtijeva pokrenut Docker daemon, jer Testcontainers
podiže SQL Server kontejner.

Flutter testovi:

```bash
cd UI/enote_core && flutter analyze && flutter test
cd UI/enote_desktop && flutter test
cd UI/enote_mobile && flutter analyze && flutter test
```

## Struktura rješenja

| Projekat | Sadržaj |
|---|---|
| `eNote.Domain` | Entiteti i enumi |
| `eNote.Application` | Poslovna logika i servisi |
| `eNote.Infrastructure` | EF Core, Identity, messaging |
| `eNote.API` | HTTP API i SignalR |
| `eNote.Worker` | RabbitMQ konzumeri i automatsko odbijanje nepreuzetih iznajmljivanja |
| `eNote.Contracts` | Ugovori poruka |
| `eNote.Tests` | Testovi |
| `UI/enote_core` | Zajednički Dart paket za oba klijenta |
| `UI/enote_test_support` | Pomoćne klase za testove, dev zavisnost sva tri Dart paketa |
| `UI/enote_desktop` | Windows desktop aplikacija |
| `UI/enote_mobile` | Android mobilna aplikacija |

## Ostala dokumentacija

- [recommender-dokumentacija.md](recommender-dokumentacija.md) opisuje recommender sistem
- [recommender-dokumentacija.zip](recommender-dokumentacija.zip) sadrži isti dokument u Word formatu, sa screenshotovima (Checklist, stavka 32)
- [docs/rs2-compliance.md](docs/rs2-compliance.md) objašnjava odluke vezane za zahtjeve predmeta
