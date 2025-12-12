# GERDA AI Overzicht

**G**eneratief **E**valuatie **R**aamwerk voor **D**ynamische **A**ssistentie

---

## Wat is GERDA?

GERDA is het AI-brein van Ticket Masala. Het automatiseert ticket dispatching door te leren van historische patronen en real-time workload.

---

## GERDA Componenten

```
               ┌─────────────────────────────────┐
               │          GERDA Engine           │
               ├─────────────────────────────────┤
               │                                 │
    ┌──────────┼─────────┬─────────┬───────────┐│
    │          │         │         │           ││
    ▼          ▼         ▼         ▼           ││
┌───────┐ ┌───────┐ ┌───────┐ ┌───────────┐   ││
│   E   │ │   R   │ │   D   │ │     A     │   ││
│Effort │ │Ranking│ │Dispatch││Anticipation│  ││
└───────┘ └───────┘ └───────┘ └───────────┘   ││
               │                               ││
               └───────────────────────────────┘│
                                                │
               └────────────────────────────────┘
```

### E - Effort Estimation
Schat de benodigde tijd/inspanning voor een ticket in op basis van:
- Ticket type en complexiteit
- Historische data
- Domein configuratie

### R - Ranking
Bepaalt welke agent het beste past bij een ticket:
- Expertise matching (domein kennis)
- Workload balancing
- Taal/regio matching

### D - Dispatching
Automatische toewijzing van tickets:
- Batch assignment met één klik
- Fallback naar round-robin
- Capaciteitslimieten respecteren

### A - Anticipation
Voorspelt toekomstige ticket instroom:
- ML.NET Time Series (SSA)
- 30-dagen forecast
- Capacity risk alerts

---

## Voorbeeld: AI Dispatch Flow

```
1. Nieuw ticket binnenkomt
         │
         ▼
2. GERDA analyseert:
   - Ticket inhoud → domein
   - Effort inschatting
   - Agent scores
         │
         ▼
3. Top 3 agent matches
   met confidence scores
         │
         ▼
4. Manager keurt goed of
   past handmatig aan
```

---

## Configuratie

GERDA wordt geconfigureerd via `config/masala_domains.yaml`:

```yaml
dispatch_strategies:
  - domain: technical
    priority_boost: 10
    required_skills: ["backend", "database"]
```

---

## Metrics

Op het **Team Dashboard** zichtbaar:
- Agent utilization %
- Forecast accuracy
- SLA compliance
- Throughput trends
