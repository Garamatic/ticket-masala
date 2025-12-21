# Ticket Masala: Architectuur voor Configuratie en Uitbreidbaarheid

**Versie:** 1.0
**Laatst Bijgewerkt:** 6 december 2025
**Status:** RFC (Request for Comments)

---

## Managementsamenvatting

Dit document definieert de architecturale blauwdruk voor de transformatie van Ticket Masala van een domeinspecifiek IT-ticketing-systeem naar een **generieke, configuratiegestuurde workflowbeheer-engine**. De kernfilosofie is:

> **"Alles wat domeinspecifiek kan zijn, MOET configuratiegestuurd zijn."**

---

## 1. Kernconcepten

### 1.1 Het Universal Entity Model

Het systeem herkent drie **universele abstracties** die in alle domeinen bestaan:

| Universeel Concept | Voorbeelden uit het Domein |
|-------------------|----------------------------|
| **Werkitem** (Work Item) | IT-ticket, belastingdossier, hoveniersbezoek, overheidsaanvraag |
| **Werkcontainer** (Work Container) | IT-project, belastingportefeuille, tuinzone, burgerdossier |
| **Werkafhandelaar** (Work Handler) | IT-agent, belastingambtenaar, horticulturist, casemanager |

Deze abstracties zijn **onveranderlijk in de code**, maar hun **labels, velden en gedragingen zijn veranderlijk via configuratie**.

### 1.2 De "In-Process" Architectuur (KISS-principe)

We vervangen infrastructuurcomponenten door .NET-abstracties om **"logische scheiding" te creëren zonder "fysieke scheiding."**

| "Enterprise" Component | "Origins" Vervanging (C#) | Waarom het werkt |
|------------------------|---------------------------|-----------------|
| **RabbitMQ** | `System.Threading.Channels` | Krachtige in-memory wachtrij. |
| **Worker Service** | `IHostedService` | Draait op een achtergrond-thread binnen dezelfde app. |
| **Redis** | `IMemoryCache` | Gebruikt container-RAM. Sneller dan via het netwerk. |
| **PostgreSQL** | `SQLite (WAL-modus)` | "Write-Ahead Logging" staat gelijktijdige lees-/schrijfacties toe. |
| **Elasticsearch** | `SQLite FTS5` | Full-text zoekmachine ingebouwd in SQLite. |

**De Exit-strategie:**
Code mag geen "SQLite-ismen" lekken. Gebruik EF Core-abstracties (bijv. `HasComputedColumnSql`), zodat een upgrade naar Postgres later slechts een kwestie is van het aanpassen van de connectionstring en provider.

### 1.3 De Configuratiehiërarchie

```text
┌─────────────────────────────────────────────────────────────────┐
│                      DOMEINCONFIGURATIE                          │
│  (YAML/JSON: masala_domain.yaml)                                 │
│  - Entiteitslabels (ticket_name: "Servicebezoek")                │
│  - Aangepaste velden (soil_ph, tax_code, os_version)              │
│  - Workflow-statussen & Overgangen                               │
│  - AI-strategieën (Ranking, Dispatching prompts)                 │
│  - Machtigingen & Rollen                                         │
├─────────────────────────────────────────────────────────────────┤
│                      WACHTRIJ-CONFIGURATIE                        │
│  (YAML/JSON: masala_queues.yaml)                                 │
│  - SLA-standaarden per wachtrij                                  │
│  - Urgentie-multipliers                                          │
│  - Regels voor automatisch archiveren                            │
├─────────────────────────────────────────────────────────────────┤
│                      GERDA AI-CONFIGURATIE                       │
│  (YAML/JSON: masala_gerda.yaml of ingebed in domein)             │
│  - G: Regels voor spamdetectie                                   │
│  - E: Categorieën voor complexiteitsinschatting                  │
│  - R: Ranking-algoritme (WSJF, risicoscore, aangepast)           │
│  - D: Dispatching-strategie (ML, regelgebaseerd, ERP-lookup)     │
│  - A: Voorpellingsparameters                                     │
├─────────────────────────────────────────────────────────────────┤
│                      INTEGRATIECONFIGURATIE                      │
│  (YAML/JSON: masala_integrations.yaml)                           │
│  - Connectoren voor externe systemen (ERP, CRM, Belasting-DB)    │
│  - Webhook-endpoints                                             │
│  - API-sleutels & Auth                                           │
│  - Ingestiemethoden (API, CSV, E-mail, ERP-synchronisatie)       │
└─────────────────────────────────────────────────────────────────┘
```

### 1.4 Integratieconfiguratie (Per Domein)

Elk domein kan zijn eigen **ingestiebronnen** en **uitgaande integraties** definiëren:

| Ingestietype | Voorbeelddomein | Beschrijving |
|--------------|-----------------|--------------|
| **API-endpoint** | Hoveniers | Formulier op externe website POST naar `/api/v1/tickets/external` |
| **ERP-sync** | Inkoop | Haal bestellingen elk kwartier uit SAP |
| **CSV-import** | HR | Wekelijkse upload van werknemersverzoeken |
| **E-mail ingestie** | IT-ondersteuning | Verwerk e-mails van <support@company.com> |
| **Webhook-push** | Overheid | Extern systeem pusht dossiers via webhook |

**Uitgaande Integraties:**

| Integratietype | Voorbeeld |
|----------------|-----------|
| **Webhook** | Verwittig ERP wanneer de ticketstatus wijzigt |
| **API-aanroep** | Werk CRM bij wanneer een ticket wordt gesloten |
| **E-mail** | Stuur een melding naar de klant |

---

## 2. Architectuur van het Gegevensmodel

### 2.1 Hybride Relationeel + JSON Mode

Om dynamische aangepaste velden te ondersteunen zonder schemamigraties, hanteert de `Ticket`-entiteit een **hybride model**:

```csharp
public class Ticket : BaseModel
{
    // ═══════════════════════════════════════════
    // UNIVERSELE VELDEN (Hardcoded, Geïndexeerd)
    // ═══════════════════════════════════════════
    public required Status TicketStatus { get; set; }
    public required string Description { get; set; }
    public string? CustomerId { get; set; }
    public string? ResponsibleId { get; set; }
    public Guid? ProjectGuid { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    
    // GERDA AI-velden (Universeel)
    public int EstimatedEffortPoints { get; set; }
    public double PriorityScore { get; set; }
    public string? GerdaTags { get; set; }
    
    // ═══════════════════════════════════════════
    // DOMEINSPECIFIEKE VELDEN (JSON-blob)
    // ═══════════════════════════════════════════
    [Column(TypeName = "jsonb")] // PostgreSQL of nvarchar(max) voor SQL Server
    public string? CustomFieldsJson { get; set; }
    
    // ═══════════════════════════════════════════
    // DOMEINCONTEXT
    // ═══════════════════════════════════════════
    public required string DomainId { get; set; } // bijv. "IT", "Hoveniers", "Belastingrecht"
    public string? WorkItemTypeCode { get; set; } // bijv. "Incident", "Servicebezoek", "Audit"
}
```

### 2.2 Opslag & Prestaties van Aangepaste Velden

Aangepaste velden worden opgeslagen als een JSON-object:

```json
{
  "soil_ph": 6.5,
  "sunlight_exposure": "Gedeeltelijke zon",
  "pest_infestation_level": "Laag",
  "last_watering_date": "2025-12-01"
}
```

> [!IMPORTANT]
> **Prestatie-eis:** Filteren op aangepaste velden (bijv. `soil_ph < 6.0`) mag geen volledige 'table scans' veroorzaken.
>
> - **PostgreSQL:** MOET het `jsonb`-kolomtype gebruiken met **GIN-indexering**.
> - **SQL Server:** MOET **berekende kolommen met indexen** gebruiken voor veelgevraagde velden.
> - **Query's:** Behandel de JSON-kolom niet als een simpele tekstblob; gebruik systeemeigen JSON-query-operators.

### 2.3 Validatie van Aangepaste Velden

Een nieuwe service valideert de JSON tegen het schema van de domeinconfiguratie tijdens runtime:

```csharp
public interface ICustomFieldValidationService
{
    ValidationResult Validate(string domainId, string customFieldsJson);
}
```

---

## 3. Service-architectuur

### 3.1 Domeinconfiguratie-service

Het **hart van de uitbreidbaarheid**. Leest en cachet domeinconfiguraties.

```csharp
public interface IDomainConfigurationService
{
    DomainConfig GetDomain(string domainId);
    IEnumerable<WorkItemTypeDefinition> GetWorkItemTypes(string domainId);
    IEnumerable<WorkflowState> GetWorkflowStates(string domainId);
    IEnumerable<TransitionRule> GetTransitions(string domainId, string fromState);
    IEnumerable<CustomFieldDefinition> GetCustomFields(string domainId);
    string? GetAiPromptTemplate(string domainId, string module); // bijv. "Ranking", "Dispatching"
}
```

### 3.2 Rule Engine-service (Gecompileerd)

Voert domeinspecifieke bedrijfsregels uit. Om de prestaties te garanderen, worden regels **bij het opstarten gecompileerd** naar 'delegates' met behulp van Expression Trees, in plaats van ze tijdens runtime te interpreteren.

```csharp
public interface IRuleEngineService
{
    // Implementatie gebruikt gecompileerde Func<WorkItem, bool> gecachet per Domein+Status
    bool CanTransition(Ticket ticket, string targetState, ClaimsPrincipal user);
}
```

**Architectuurpatroon:** Specification Pattern + Expression Trees.
**Prestaties:** Vermijdt herhaaldelijke JSON-parsing en tekst-evaluatie.

**Voorbeeldregel (YAML):**

```yaml
overgangen:
  WachtOpOfferte:
    - naar: OfferteVerzonden
      voorwaarden:
        - veld: geoffreerde_prijs
          operator: is_niet_leeg
        - rol: CFO
```

### 3.3 GERDA AI-strategie Factory

Injecteert dynamisch de juiste AI-strategie op basis van de domeinconfiguratie.

### 3.4 GERDA AI-architectuur

Transformeert van een eenvoudige strategiekeuze naar een **Feature Extraction Pipeline**.

1.  **Strategy Factory (`IStrategyFactory`):** Resolvert de geconfigureerde strategie-implementatie voor het domein (bijv. het laden van `MatrixFactorization` voor toewijzing in IT).
2.  **Feature Extraction (`IFeatureExtractor`):** Extraheert en normaliseert gegevens (uit JSON of SQL) naar een vector.
    - Geïmplementeerd door `DynamicFeatureExtractor`, aangestuurd door `masala_domains.yaml`.
    - Ondersteunt transformaties: `min_max`, `one_hot`, `bool`.
3.  **Inference:** Strategieën gebruiken de geëxtraheerde kenmerken om modellen uit te voeren (ML.NET/ONNX) of heuristische logica.

```csharp
public interface IFeatureExtractor
{
    // Mapt Ticket + Config -> Float[]
    float[] ExtractFeatures(Ticket ticket, GerdaModelConfig config);
}
```

**Geregistreerde Strategieën (via DI):**

| Domeintype | Service-interface | Implementatie | Config Key |
|------------|-------------------|----------------|------------|
| Ranking | `IJobRankingStrategy` | `WeightedShortestJobFirstStrategy` | `WSJF` |
| Inschatting | `IEstimatingStrategy` | `CategoryBasedEstimatingStrategy` | `CategoryLookup` |
| Toewijzing | `IDispatchingStrategy` | `MatrixFactorizationDispatchingStrategy` | `MatrixFactorization` |

---

## 4. Configuratievoorbeelden

### 4.1 Master Domeinconfiguratie

**Bestand:** `masala_domains.yaml`

```yaml
domeinen:
  # ════════════════════════════════════════════════════════════
  # IT-ONDERSTEUNINGSDOMEIN
  # ════════════════════════════════════════════════════════════
  IT:
    display_name: "IT-ondersteuning"
    entity_labels:
      work_item: "Ticket"
      work_container: "Project"
      work_handler: "Agent"
    
    work_item_types:
      - code: INCIDENT
        name: "Incident"
        icon: "fa-fire"
        default_sla_days: 1
      - code: SERVICE_REQUEST
        name: "Serviceverzoek"
        icon: "fa-cogs"
        default_sla_days: 5
      - code: PROBLEM
        name: "Probleem"
        icon: "fa-exclamation-triangle"
        default_sla_days: 14
    
    custom_fields:
      - name: beinvloede_systemen
        type: multi_select
        options: ["E-mail", "VPN", "ERP", "CRM", "Andere"]
        required: false
      - name: os_versie
        type: text
        required: false
    
    workflow:
      states: [Nieuw, InBehandeling, InWacht, Opgelost, Gesloten]
      transitions:
        Nieuw: [InBehandeling, Gesloten]
        InBehandeling: [InWacht, Opgelost]
        InWacht: [InBehandeling]
        Opgelost: [Gesloten, InBehandeling] # Heropenen toegestaan
        Gesloten: [] # Eindentiteit
    
    ai_strategies:
      ranking: WSJF
      dispatching: MatrixFactorization
      estimating: CategoryLookup
    
    ai_prompts:
      summarize: "Schat dit IT-ticket in en doe suggesties voor probleemoplossing."
    
    # ─────────────────────────────────────────
    # INTEGRATIECONFIGURATIE
    # ─────────────────────────────────────────
    integrations:
      ingestion:
        - type: email
          enabled: true
          config:
            mailbox: "support@company.com"
            protocol: IMAP
            polling_interval_minutes: 5
        - type: api
          enabled: true
          config:
            endpoint: "/api/v1/tickets/external"
            auth: api_key
      
      outbound:
        - type: webhook
          trigger: on_status_change
          config:
            url: "https://erp.company.com/tickets/update"
            method: POST
        - type: email
          trigger: on_resolved
          config:
            template: "ticket_resolved"
            to: "{{customer.email}}"
  
  # ════════════════════════════════════════════════════════════
  # HOVENIERSDOMEIN
  # ════════════════════════════════════════════════════════════
  Gardening:
    display_name: "Hoveniersdiensten"
    entity_labels:
      work_item: "Servicebezoek"
      work_container: "Tuinzone"
      work_handler: "Horticulturist"
    
    work_item_types:
      - code: QUOTE_REQUEST
        name: "Offerteaanvraag"
        icon: "fa-leaf"
        default_sla_days: 3
      - code: SCHEDULED_MAINTENANCE
        name: "Gepland Onderhoud"
        icon: "fa-calendar"
        default_sla_days: 7
      - code: PEST_CONTROL
        name: "Plagenbestrijding"
        icon: "fa-bug"
        default_sla_days: 2
    
    custom_fields:
      - name: soil_ph
        type: number
        min: 0
        max: 14
        required: false
      - name: sunlight_exposure
        type: select
        options: ["Volle zon", "Halfschaduw", "Volle schaduw"]
        required: true
      - name: plant_species
        type: text
        required: false
      - name: pest_type
        type: text
        required_for_types: [PEST_CONTROL]
    
    workflow:
      states: [Aangevraagd, Geoffreerd, Gepland, InUitvoering, Voltooid, Geannuleerd]
      transitions:
        Aangevraagd: [Geoffreerd, Geannuleerd]
        Geoffreerd: [Gepland, Geannuleerd]
        Gepland: [InUitvoering, Geannuleerd]
        InUitvoering: [Voltooid]
        Voltooid: []
        Geannuleerd: []
    
    ai_strategies:
      ranking: SeasonalPriority
      dispatching: ZoneBased
      estimating: GardenComplexity
    
    ai_prompts:
      summarize: "Vat dit hoveniersverzoek samen. Let op eventuele zorgen over de gezondheid van planten."
    
    # ─────────────────────────────────────────
    # INTEGRATIECONFIGURATIE
    # ─────────────────────────────────────────
    integrations:
      ingestion:
        - type: api
          enabled: true
          config:
            endpoint: "/api/v1/tickets/external"
            source_filter: "greenscape-landscaping"
            auth: api_key
      
      outbound:
        - type: webhook
          trigger: on_status_change
          config:
            url: "https://greenscape.com/api/job-status"
            method: POST
        - type: email
          trigger: on_quoted
          config:
            template: "quote_ready"
            to: "{{customer.email}}"
  # ════════════════════════════════════════════════════════════
  # OVERHEID / BELASTINGRECHTDOMEIN
  # ════════════════════════════════════════════════════════════
  TaxLaw:
    display_name: "Beheer van Belastingzaken"
    entity_labels:
      work_item: "Dossier"
      work_container: "Portefeuille"
      work_handler: "Dossierbeheerder"
    
    work_item_types:
      - code: DISPUTE
        name: "Belastinggeschil"
        icon: "fa-gavel"
        default_sla_days: 90
      - code: REFUND
        name: "Teruggaveverzoek"
        icon: "fa-money-bill"
        default_sla_days: 30
      - code: AUDIT
        name: "Controle / Audit"
        icon: "fa-search"
        default_sla_days: 180
    
    custom_fields:
      - name: belastingcode_referentie
        type: text
        required: true
      - name: dossier_waarde
        type: currency
        required: true
      - name: audit_status
        type: select
        options: ["Niet gestart", "In beoordeling", "Geëscaleerd", "Afgerond"]
        required_for_types: [AUDIT]
    
    workflow:
      states: [Ingediend, InBeoordeling, WachtenOpDocumenten, Geëscaleerd, Opgelost, Gesloten]
      transitions:
        Ingediend: [InBeoordeling]
        InBeoordeling: [WachtenOpDocumenten, Geëscaleerd, Opgelost]
        WachtenOpDocumenten: [InBeoordeling]
        Geëscaleerd: [Opgelost]
        Opgelost: [Gesloten]
        Gesloten: []
      
      transition_rules:
        - from: InBeoordeling
          to: Geëscaleerd
          conditions:
            - veld: dossier_waarde
              operator: ">"
              value: 100000
            - rol: SeniorOfficer
    
    ai_strategies:
      ranking: Risicoscore
      dispatching: ExpertiseMatch
      estimating: LegalComplexity
    
    ai_prompts:
      summarize: "Vat deze belastingzaak samen. Citeer indien van toepassing relevante wetsartikelen."
```

---

## 5. Implementatiefasen

### Fase 1: Configuratie-infrastructuur (Huidig)

- [ ] Implementeer `IDomainConfigurationService` om `masala_domains.yaml` te lezen
- [ ] Maak `DomainConfig`, `WorkItemTypeDefinition`, `CustomFieldDefinition` modellen
- [ ] Voeg `DomainId` en `CustomFieldsJson` toe aan de `Ticket`-entiteit + migratie
- [ ] Werk de `Ticket/Create`-view bij om gebruik te maken van geconfigureerde werkitemtypes

### Fase 2: Workflow-engine

- [ ] Implementeer `IRuleEngineService` voor validatie van statusovergangen
- [ ] Werk `TicketService` bij om overgangsregels uit de configuratie af te dwingen
- [ ] Voeg UI-indicatoren toe voor geldige vervolgstatussen

### Fase 3: GERDA AI Uitbreidbaarheid

- [ ] Implementeer `IGerdaStrategyFactory`
- [ ] Maak alternatieve ranking-strategieën (Risicoscore, Seizoensgebonden Prioriteit)
- [ ] Voeg domeinbewuste prompt-sjablonen toe aan GERDA-modules

### Fase 4: ERP/Externe Integratie

- [ ] Definieer de `IExternalDataConnector`-interface
- [ ] Implementeer 'stub'-connectoren voor testdoeleinden
- [ ] Voeg webhook-dispatch toe bij statusovergangen

---

## 6. Migratiestrategie

Om bestaande gegevens te behouden tijdens de transitie:

1. **Databasemigratie:**
   - Voeg `DomainId`-kolom toe met standaardwaarde `"IT"` (bestaande tickets worden onderdeel van het IT-domein)
   - Voeg `CustomFieldsJson`-kolom toe (nullable)
   - Voeg `WorkItemTypeCode`-kolom toe

2. **Gegevensaanvulling:**
   - Map bestaande `TicketType` enum-waarden naar nieuwe `WorkItemTypeCode` tekstwaarden
   - Migreer optioneel specifieke kolommen naar `CustomFieldsJson`

3. **Code Opschonen (Later):**
   - Schaf de `TicketType` enum af ten gunste van de tekstwaarde `WorkItemTypeCode`

---

## 7. Beveiligingsoverwegingen

- **Toegang tot configuratiebestanden:** YAML-bestanden moeten worden beschermd (niet in wwwroot)
- **Injectie via aangepaste velden:** JSON-invoer moet worden geschoond (sanitized)
- **Rolvalidatie:** Overgangsregels die naar rollen verwijzen, moeten worden gevalideerd tegen ASP.NET Identity

---

## 8. Open Vragen & Aanbevelingen

### 8.1 Warm Herladen (Hot Reload)

**Vraag:** Moet de app opnieuw worden opgestart na configuratiewijzigingen?

**Aanbeveling:** Nee. Implementeer een caching-laag met handmatige ongeldigverklaring.

**Implementatie:**
- De huidige `DomainConfigurationService` heeft al een `ReloadConfiguration()` methode.
- Voeg een admin-only `/api/config/reload` endpoint toe.
- Voeg optioneel een 'file watcher' toe voor automatische detectie van YAML-wijzigingen.

### 8.2 Multi-Tenancy

**Vraag:** Is een domein synoniem aan een tenant?

**Aanbeveling:** Nee, ontkoppel ze:
- **Tenant** = Organisatie (Bedrijf A vs Bedrijf B)
- **Domein** = Bedrijfsproces binnen een tenant (IT vs HR)

**Implementatie:**
- Voeg een `TenantId`-kolom toe aan `Ticket`, `Project`, enz.
- Filter op `TenantId` in alle service-aanroepen.
- Overweeg implementatie in een latere fase nadat 'single-tenant' stabiel is.

### 8.3 Versiebeheer & Snapshot-strategie voor Configuratie (CRITIEK)

**Het risico:** Het wijzigen van een regel in `masala_domains.yaml` kan de validatie breken voor bestaande tickets die onder eerdere regels zijn aangemaakt.

**De oplossing:** Snapshot-strategie.

1. **DomainConfigVersion Entiteit:** Slaat onveranderlijke snapshots van de configuratie op.
2. **Koppeling met Ticket:** De `Ticket`-entiteit slaat de `ConfigVersionId` op bij aanmaak.
3. **Rule Engine:** Vraagt de *specifieke versie* van de regels op die bij het ticket horen, niet alleen de "Live" versie.
4. **Lazy Compilation Cache:** `Dictionary<(string DomainId, string VersionHash), CompiledPolicy>`.

**Aanbeveling:** **VERPLICHT** voor Fase 5 (Rule Compiler).

---

*Dit document is onderhevig aan herziening en iteratie.*
