# Gatekeeper API: Schaalbare Ingestie-service

**Een minimale API voor de verwerking van grote hoeveelheden gegevens**

---

## Doel

De **Gatekeeper API** is een aparte, lichtgewicht service die is ontworpen voor de **schaalbare ingestie** van werkitems (tickets) in Ticket Masala. Het biedt:

- **Asynchrone verwerking** via `System.Threading.Channels`.
- **Hoge doorvoer** voor bulk-imports.
- **Ontkoppelde architectuur** (kan onafhankelijk worden geschaald).
- **Eenvoudige API** (één POST-endpoint).

---

## Architectuur

De Gatekeeper API ontvangt gegevens van externe systemen (via CSV, e-mail of webhooks) via een POST-verzoek op `/api/ingest`. De gegevens worden in een interne wachtrij (Channel) geplaatst, waarna een achtergrond-worker de items één voor één verwerkt, transformeert en opslaat in de Ticket Masala database.

---

## Componenten

### 1. IngestionQueue<T>
Een in-memory wachtrij die gebruikmaakt van .NET Channels voor maximale prestaties en thread-safety zonder de overhead van een externe message broker.

### 2. IngestionWorker
Een achtergrondservice die items uit de wachtrij haalt, ze valideert, naar het domeinmodel transformeert en opslaat.

### 3. API Endpoint (`POST /api/ingest`)
Accepteert payloads direct en retourneert onmiddellijk een `202 Accepted` status, waardoor de verzender niet hoeft te wachten op de volledige verwerking.

---

## Gebruik

U kunt gegevens verzenden via een eenvoudig HTTP POST-verzoek met een JSON-body die de titel, beschrijving en het domein-ID van het nieuwe ticket bevat.

```bash
curl -X POST http://localhost:5000/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Nieuw Ticket",
    "description": "Beschrijving van het probleem",
    "domainId": "IT"
  }'
```

---

## Configuratie en Roadmap

De huidige implementatie is een minimaal skelet. Geplande verbeteringen omvatten:
- Uitgebreide payload-transformatie en validatie.
- Directe integratie met de Ticket Masala database.
- Foutafhandeling met 'dead letter' wachtrijen en automatische herbewerking.

---

## Vergelijking: Gatekeeper vs Hoofd-app Ingestie

| Aspect | Gatekeeper API | Hoofd-app Ingestie |
|--------|---------------|-------------------|
| **Doel** | Bulk / Hoge doorvoer | Interactief / Gebruikersgestuurd |
| **Latentie** | Accepteert direct | Blokkeert tot opslag voltooid is |
| **Schaalbaarheid** | Kan onafhankelijk schalen | Schaalt met de hoofd-app |

---

## Gerelateerde Documentatie

- [Gedetailleerde Architectuur](../architecture/DETAILED.md)
- [Tenants vs Domeinen](TENANTS_VS_DOMAINS.md)
- [Configuratiegids](CONFIGURATION.md)
