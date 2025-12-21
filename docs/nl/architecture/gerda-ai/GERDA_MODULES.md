# GERDA AI-modules

Volledige documentatie voor het GERDA AI-systeem in Ticket Masala.

## Overzicht

**GERDA** = **G**roups (Groepeert), **E**stimates (Schat in), **R**anks (Rangschikt), **D**ispatches (Verzendt), **A**nticipates (Anticipeert)

GERDA is de door AI aangedreven automatiseringspijplijn die tickets verwerkt en intelligente aanbevelingen doet.

```
Nieuw ticket aangemaakt
        ↓
┌───────────────────────────────────────────┐
│              GERDA-pijplijn               │
├───────────────────────────────────────────┤
│ G │ Grouping    → Spamdetectie, samenvoegen │
│ E │ Estimating  → Inspanningspunten         │
│ R │ Ranking     → Prioriteitsscore (WSJF)   │
│ D │ Dispatching → Aanbeveling medewerker    │
│ A │ Anticipation → Capaciteitsvoorspelling  │
└───────────────────────────────────────────┘
        ↓
Ticket verrijkt met AI-gegevens
```

---

## Architectuur

### GerdaService (Orchestrator)

**Locatie:** `Engine/GERDA/GerdaService.cs`

Het hoofdingangspunt dat alle GERDA-modules coördineert.

```csharp
public class GerdaService : IGerdaService
{
    private readonly IGroupingService _groupingService;
    private readonly IEstimatingService _estimatingService;
    private readonly IRankingService? _rankingService;
    private readonly IDispatchingService? _dispatchingService;
    private readonly IAnticipationService? _anticipationService;

    public async Task ProcessTicketAsync(Guid ticketGuid)
    {
        // G - Grouping
        await _groupingService.CheckAndGroupTicketAsync(ticketGuid);
        
        // E - Estimating
        await _estimatingService.EstimateComplexityAsync(ticketGuid);
        
        // R - Ranking
        if (_rankingService?.IsEnabled == true)
            await _rankingService.CalculatePriorityScoreAsync(ticketGuid);
        
        // D - Dispatching
        if (_dispatchingService?.IsEnabled == true)
            await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
    }
}
```

---

## G - Grouping (Ruijsfilter)

Detecteert en voegt dubbele of spam-tickets samen.

### Implementatie

**Interface:** `IGroupingService`  
**Locatie:** `Engine/GERDA/Grouping/`

**Algoritme:**
1. Bereken `SHA256(Description + CustomerId)` als inhouds-hash.
2. Zoek naar bestaande tickets met dezelfde hash binnen een bepaald tijdsbestek.
3. Indien een match wordt gevonden, markeer als sub-ticket/duplicaat.

---

## E - Estimating (Inschatting)

Kent inspanningspunten toe op basis van ticketkenmerken.

### Implementatie

**Interface:** `IEstimatingService`  
**Locatie:** `Engine/GERDA/Estimating/`

**Algoritme:**
1. Analyseer de ticketbeschrijving op trefwoorden.
2. Vergelijk met de mapping van categorie en complexiteit.
3. Ken Fibonacci-punten toe (1, 2, 3, 5, 8, 13).

---

## R - Ranking (Rangschikking)

Berekent prioriteitsscores met behulp van WSJF (Weighted Shortest Job First).

### Implementatie

**Interface:** `IRankingService`  
**Locatie:** `Engine/GERDA/Ranking/`

**Algoritme (WSJF):**
```
Prioriteit = (Kosten van vertraging × SLA-gewicht) / (Omvang van de taak × Complexiteitsgewicht)

Factoren voor kosten van vertraging:
- Bedrijfswaarde
- Tijdkritiekheid (dagen tot SLA-schending)
- Risicoreductie
```

---

## D - Dispatching (Matchmaker)

Beveelt de beste medewerker aan voor elk ticket.

### Implementatie

**Interface:** `IDispatchingService`  
**Locatie:** `Engine/GERDA/Dispatching/`

**Strategieën:**
- `MatrixFactorization` - ML-gebaseerde samenwerkingsfiltering.
- `ZoneBased` - Geografische toewijzing.
- `ExpertiseMatch` - Vaardigheidsmatching via FTS5.

---

## A - Anticipation (Voorspelling)

Voorspelt toekomstig ticketvolume en capaciteitsrisico's.

### Implementatie

**Interface:** `IAnticipationService`  
**Locatie:** `Engine/GERDA/Anticipation/`

**Algoritme:**
- ML.NET SSA (Singular Spectrum Analysis).
- Analyseert historische instroompatronen.
- Detecteert seizoensgebonden trends.

---

## Achtergrondverwerking

### GerdaBackgroundJob

Voert periodieke GERDA-verwerking uit op alle openstaande tickets.

**Registratie:**
```csharp
services.AddHostedService<GerdaBackgroundJobService>();
```

### Verwerking via Wachtrij

Nieuwe tickets worden verwerkt via een achtergrondwachtrij:

```csharp
await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
{
    await gerdaService.ProcessTicketAsync(ticketGuid);
});
```

---

## Belangrijkste Ontwerpbeslissingen

| Beslissing | Ratio |
|----------|-----------|
| In-process ML.NET | GDPR-privacy, geen API-kosten |
| Configuratiegestuurd | Gedrag wijzigen zonder herinstallatie |
| Achtergrondverwerking | Blokkeer UI-threads niet |
| Optionele services | Geleidelijke degradatie indien uitgeschakeld |
| Inhoud-hashing | O(1) duplicaatdetectie |

---

## Verdere Informatie

- [Configuratiegids](../../guides/CONFIGURATION.md) - GERDA-instellingen
- [Architectuuroverzicht](../SUMMARY.md) - Systeemontwerp
- [Observer-patroon](../OBSERVERS.md) - Event-gestuurde verwerking
