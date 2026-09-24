# Odluke vezane za zahtjeve predmeta

Ovdje su objašnjenja odluka koje bi na pregledu mogle izgledati kao propust, a
napravljene su namjerno. Uputstvo za pokretanje i prijavu je u
[README.md](../README.md).

## Referentni podaci

Administrator ima CRUD ekrane za sve referentne tabele u domenu:

| Tabela | Ekran |
|---|---|
| Gradovi | `admin/cities` |
| Adrese | `admin/addresses` |
| Muzičke radnje | `admin/music-stores` |
| Vrste instrumenata | `admin/instrument-types` |

`Address.CityId` je strani ključ prema tabeli gradova i bira se kroz dropdown, ne
kao slobodan tekst.

`Instructor` je namjerno samo za čitanje u `InstructorListScreen`. Administrator
kreira instruktorske naloge kroz `AdminUsersController` i `UserProvisionFormScreen`,
gdje se postavlja i uloga. Administrator daje samo pristup: lične podatke
(ime, prezime, e-mail, telefon) svaki korisnik uređuje sam na svom profilu
(`PUT users/me`).

Tabele `Country`, `Category` i `Status` ne postoje. Sve adrese su unutar BiH i
imaju oblik grad, ulica, broj, a instrumenti nemaju kategorije odvojene od vrste.
Prazne tabele bez upita, filtera ili izvještaja koji ih koriste bile bi
normalizacija bez svrhe.

## Rang lista

Rang lista kursa je ograničena lista: API vraća 15 najboljih studenata
(`RankingService.RankingTopCount`), poredanih po prosjeku ocjena, pa po ID-u
studenta. Student koji je ispod 15. mjesta dobije i svoj red na kraju liste,
sa stvarnim mjestom (npr. 20.), a mobilna aplikacija prije njega prikaže "…".
PDF izvještaj za instruktora sadrži sve ocijenjene studente, jer se koristi za
zaključne ocjene.

## Pretraga na listama

`RankingScreen` ima filter po imenu studenta iznad tabele, sa odgodom pri kucanju.
Filter radi nad već dohvaćenim podacima rangiranja, pa nema dodatnog poziva prema
API-ju.

Tri liste namjerno nemaju parametar za pretragu. Uputstvo dozvoljava izuzetak
kada je opravdan, a ovdje pretraga ne bi pomogla:

| Lista | Endpoint | Razlog |
|---|---|---|
| Prisustvo na predavanju (desktop, instruktor) | `GET instructor/lectures/{id}/attendance` | Vezana je za jedno predavanje i ima po jedan red za svakog studenta na njemu. |
| Predaje za zadatak (desktop, instruktor) | `GET instructor/lectures/{lectureId}/assignments/{assignmentId}/submissions` | Vezana je za jedan zadatak i ima najviše jednu predaju po studentu. |
| Historija predaja (mobilna, student) | `GET student/submissions` | Student vidi samo svoje predaje, samo iz kurseva kojima ima pristup, od najnovije, sa straničenjem. |

Search objekti ovih endpointa (`AttendanceSearchObject`, `SubmissionSearchObject`,
`AssignmentSubmissionSearchObject`) zato nose samo straničenje iz
`BaseSearchObject`.

## Poruke validacije

Polja sa formatom nose konkretnu poruku, ne samo oznaku da su obavezna. Odnosi se
na `City.Name`, `Address.CityId`, `Address.Street`, `Address.Number`,
`MusicStore.StoreName`, `MusicStore.BusinessHours`, `InstrumentType.Type`,
`InstrumentType.MonthlyFee`, `Course.Name` i `Course.Price`. Tačni tekstovi su u
`*RequestValidator` klasama, u pozivima `WithMessage`.

## Navigacija unazad

Liste su smještene u drawer kroz `MasterScreen` i `RoleMenu`. Prva stavka
menija za ulogu je početni ekran (administrator: "Korisnici", instruktor:
"Kursevi", zaposlenik prodavnice: "Moja prodavnica"). Dugme "Nazad" iznad
sadržaja vraća sa bilo kojeg drugog ekrana na početni ekran. Na početnom ekranu
dugme je onemogućeno, a tooltip objašnjava razlog ("Ovo je početni ekran").
Dijalozi (npr. "Referentni podaci") se zatvaraju dugmetom u gornjem desnom uglu.
Ekrani detalja koji se otvaraju kao nova ruta imaju svoju strelicu nazad u
`AppBar`.

## Veze više prema više

Jedina veza više prema više je između kursa i studenta, kroz `Enrollment`. Ekrani
rangiranja i prisustva prikazuju imena studenata, nikad ID vrijednosti.

## Cijena kursa i pristup sadržaju

`Course.Price` je mjesečna cijena kursa; cijena 0 označava besplatan kurs.
Student je plaća unaprijed i time dobija 30 dana pristupa sadržaju tog kursa, a
obnova je ručna.

Pristup se provjerava dvama nezavisnim uslovima: članstvo
(`Student.MembershipPaidUntil`) je datumsko, na dan, i odlučuje samo da li se
student smije upisati, dok je školarina (`Enrollment.PaidUntil`) tačan UTC
trenutak i odlučuje samo da li student vidi sadržaj upisanog kursa. Razlika u
granularnosti je namjerna.

## Slike na formama

Sličice su 40 piksela, što je oko 5 posto širine forme.

## Lozinke seed naloga

Seed nalozi imaju lozinku `test`, kako traži tabela pristupnih podataka u
uputstvu. Politika lozinki u aplikaciji ostaje stroga: najmanje 8 znakova, veliko
i malo slovo, broj i specijalni znak. To je riješeno tako što `IdentitySeed`
kreira nalog kroz redovan tok, a zatim direktno postavlja hash lozinke za
pregled. Registracija kroz aplikaciju i dalje prolazi punu provjeru.
