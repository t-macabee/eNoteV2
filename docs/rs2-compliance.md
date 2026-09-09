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

`Instructor` je namjerno samo za čitanje u `InstructorListScreen`. Instruktori se
kreiraju i uređuju kroz `AdminUsersController` i `UserProvisionFormScreen`, gdje
se postavlja i uloga. Drugi CRUD nad istim nalozima bi se vremenom razišao sa
prvim.

Tabele `Country`, `Category` i `Status` ne postoje. Sve adrese su unutar BiH i
imaju oblik grad, ulica, broj, a instrumenti nemaju kategorije odvojene od vrste.
Prazne tabele bez upita, filtera ili izvještaja koji ih koriste bile bi
normalizacija bez svrhe.

## Pretraga na listama

`RankingScreen` ima filter po imenu studenta iznad tabele, sa odgodom pri kucanju.
Filter radi nad već dohvaćenim podacima rangiranja, pa nema dodatnog poziva prema
API-ju.

## Poruke validacije

Polja sa formatom nose konkretnu poruku, ne samo oznaku da su obavezna. Odnosi se
na `City.Name`, `Address.CityId`, `Address.Street`, `Address.Number`,
`MusicStore.StoreName`, `MusicStore.BusinessHours`, `InstrumentType.Type`,
`InstrumentType.MonthlyFee`, `Course.Name` i `Course.Price`. Tačni tekstovi su u
`*RequestValidator` klasama, u pozivima `WithMessage`.

## Navigacija unazad

Liste su smještene u drawer kroz `MasterScreen` i `RoleMenu`, a ne kao rute na
steku. Drawer je primarna navigacija. Forme imaju dugme za zatvaranje u gornjem
desnom uglu. Dugme "Nazad" na listama nije dodano jer nema rute na koju bi se
vratilo, pa bi obećavalo ponašanje koje ne postoji.

## Veze više prema više

Jedina veza više prema više je između kursa i studenta, kroz `Enrollment`. Ekrani
rangiranja i prisustva prikazuju imena studenata, nikad ID vrijednosti.

## Slike na formama

Sličice su 40 piksela, što je oko 5 posto širine forme.

## Lozinke seed naloga

Seed nalozi imaju lozinku `test`, kako traži tabela pristupnih podataka u
uputstvu. Politika lozinki u aplikaciji ostaje stroga: najmanje 8 znakova, veliko
i malo slovo, broj i specijalni znak. To je riješeno tako što `IdentitySeed`
kreira nalog kroz redovan tok, a zatim direktno postavlja hash lozinke za
pregled. Registracija kroz aplikaciju i dalje prolazi punu provjeru.
