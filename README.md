# eNote

Platforma za muzičku školu. Pokriva kurseve, predavanja, zadatke, najam
instrumenata i notifikacije. Sastoji se od ASP.NET Core API-ja, zasebnog worker
servisa, Flutter desktop aplikacije za administraciju i Flutter mobilne
aplikacije za studente.

## Preduslovi

- [.NET 10 SDK](https://dotnet.microsoft.com/download), verzija je zaključana u `global.json`
- Docker Desktop, ako aplikaciju pokrećete kroz `docker compose`
- Flutter SDK, za desktop i mobilnu aplikaciju
- SQL Server i RabbitMQ, samo ako sve pokrećete lokalno bez Dockera

## Pokretanje kroz Docker

Ovo je najkraći put i pokreće bazu, RabbitMQ, API i worker odjednom.

1. Napravite `.env` u korijenu repozitorija:

   ```bash
   cp .env.docker.example .env
   ```

   Popunite `MSSQL_SA_PASSWORD`, `JWT__KEY` (najmanje 32 znaka), `SMTP_*` i
   `STRIPE_*` vrijednosti. `docker compose` neće startati bez njih.

2. Pokrenite sve servise:

   ```bash
   docker compose up --build
   ```

API sluša na `http://localhost:5059`. Migracije i seed podaci se primjenjuju
automatski pri prvom pokretanju u Development okruženju.

## Pokretanje bez Dockera

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

Worker je zaseban projekat i zaseban kontejner. On preuzima poruke sa RabbitMQ-a
i šalje notifikacije i mailove, te ponavlja neuspjele notifikacije o najmu.

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

## Plaćanja

Najam instrumenta se naplaćuje jednom, kada najam pređe u status `Complete` ili
`ReturnedEarly`. Iznos računa server. Podrazumijevana valuta je BAM i mijenja se
kroz `STRIPE_CURRENCY`.

Obavezne varijable su `STRIPE_SECRET_KEY` i `STRIPE_WEBHOOK_SECRET`. Webhook
treba usmjeriti na `POST /api/v1/payments/stripe/webhook`, lokalno preko
`stripe listen`.

| Endpoint | Uloga |
|---|---|
| `POST /api/v1/student/rentals/{rentalId}/payments/create-intent` | Student |
| `GET /api/v1/student/rentals/{rentalId}/payments` | Student |
| `POST /api/v1/shop/rentals/{rentalId}/payments/refund` | StoreEmployee |
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

Flutter testovi:

```bash
cd UI/enote_core && flutter test
cd UI/enote_desktop && flutter test
```

## Struktura rješenja

| Projekat | Sadržaj |
|---|---|
| `eNote.Domain` | Entiteti i enumi |
| `eNote.Application` | Poslovna logika i servisi |
| `eNote.Infrastructure` | EF Core, Identity, messaging |
| `eNote.API` | HTTP API i SignalR |
| `eNote.Worker` | RabbitMQ konzumeri |
| `eNote.Contracts` | Ugovori poruka |
| `eNote.Tests` | Testovi |
| `UI/enote_core` | Zajednički Dart paket za oba klijenta |
| `UI/enote_desktop` | Windows desktop aplikacija |
| `UI/enote_mobile` | Android mobilna aplikacija |

## Ostala dokumentacija

- [recommender-dokumentacija.md](recommender-dokumentacija.md) opisuje recommender sistem
- [docs/rs2-compliance.md](docs/rs2-compliance.md) objašnjava odluke vezane za zahtjeve predmeta
