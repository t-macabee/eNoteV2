# eNote — Dokumentacija recommender sistema

## Pregled

eNote koristi hibridni recommender za preporuku instrumenata studentima. Implementacija se nalazi u sloju aplikacije i koristi stvarne podatke iz baze (historija najma, pregledi kataloga, globalna popularnost).

## Algoritam i težine

| Signal | Težina | Opis |
|--------|--------|------|
| Historija najma | 40% | Preferira tipove instrumenata koje je student već najmio; uključuje kolaborativni signal (instrumenti koje biraju slični studenti) |
| Pregledi | 30% | Instrumenti koje je student pregledao, normalizovano po max broju pregleda |
| Sličnost | 20% | Isti proizvođač (1.0) ili isti tip (0.6) u odnosu na preferirani profil |
| Popularnost | 10% | Broj globalnih najmova instrumenta, normalizovano |

Ukupni skor:

```
total = rental*0.40 + view*0.30 + similarity*0.20 + popularity*0.10
```

## Glavna logika (source code)

- Servis: `eNote.Application/Features/Recommendations/Services/RecommendationService.cs`
- API endpoint: `GET /api/student/instruments/recommended?count=5`
- Evidencija pregleda: `POST /api/student/instruments/{id}/view` → tabela `InstrumentView`

## Objašnjive preporuke

Svaka preporuka vraća `reasons[]` na bosanskom (npr. historija najma, slični studenti, popularnost). Poruke se generišu u metodi `BuildReasons`.

## Primjer odgovora API-ja

```json
{
  "instrument": { "id": 3, "model": "AC15C1", "manufacturer": "Vox" },
  "score": 0.7421,
  "reasons": [
    "Na osnovu vaše historije najma (Limeni).",
    "Popularan među studentima."
  ]
}
```

## Podaci koji se prikupljaju u aplikaciji

| Podatak | Tabela | Kako se puni |
|---------|--------|--------------|
| Historija najma | `InstrumentRental` | Tokom rental workflow-a |
| Pregledi | `InstrumentView` | `POST .../instruments/{id}/view` |
| Popularnost | `InstrumentRental` (agregat) | Automatski iz postojećih najmova |

## Napomena za odbranu

Svi signali u scoring formuli se aktivno koriste u kodu — nema prikupljanja podataka koji se zatim ignorišu.