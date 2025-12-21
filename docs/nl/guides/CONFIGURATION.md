# Ticket Masala Configuratiegids

Volledige handleiding voor het configureren van Ticket Masala voor uw specifieke toepassing.

## Inhoudsopgave

1. [Omgevingsvariabelen](#omgevingsvariabelen)
2. [Configuratiebestanden](#configuratiebestanden)
3. [Domeinconfiguratie](#domeinconfiguratie)
4. [GERDA AI-instellingen](#gerda-ai-instellingen)
5. [Seed-data](#seed-data)
6. [Voorbeelden](#voorbeelden)

---

## Omgevingsvariabelen

### MASALA_CONFIG_PATH

Overschrijf de standaard configuratiemap.

**Standaardgedrag:**
- Ontwikkeling: `./config` (relatief aan de projectroot)
- Docker: `/app/config`

**Gebruik:**
```bash
# Linux/Mac
export MASALA_CONFIG_PATH=/pad/naar/uw/config

# Windows
set MASALA_CONFIG_PATH=C:\pad\naar\uw\config
```

---

## Configuratiebestanden

Alle configuratiebestanden worden geladen uit de map die is opgegeven door `MASALA_CONFIG_PATH`:

```
config/
├── masala_domains.yaml    # Domeindefinities (vereist)
├── masala_config.json     # GERDA AI-instellingen (vereist)
└── seed_data.json         # Database seed-data (optioneel)
```

### masala_config.json

Hoofdconfiguratiebestand van de applicatie. Bevat instellingen voor de applicatienaam, beschrijving en de configuratie van de verschillende GERDA AI-modules (Groepeer, Schatting, Rangschikking, Verzending en Anticipatie).

---

## Domeinconfiguratie

Definieer werkdomeinen (zoals IT-support, Tuinonderhoud, etc.) met aangepaste workflows en AI-strategieën in `masala_domains.yaml`.

### Een aangepast domein aanmaken

1. **Entiteitslabels definiëren** (terminologie aanpassen):
```yaml
entity_labels:
  work_item: "Serviceverzoek"      # In plaats van "Ticket"
  work_container: "Tuinzone"       # In plaats van "Project"
  work_handler: "Horticulturist"   # In plaats van "Agent"
```

2. **Werkitemtypen definiëren**:
```yaml
work_item_types:
  - code: OFFERTE_AANVRAAG
    name: "Offerte-aanvraag"
    icon: "fa-leaf"
    color: "#28a745"
    default_sla_days: 3
```

3. **Aangepaste velden definiëren**:
```yaml
custom_fields:
  - name: bodem_ph
    label: "pH-waarde bodem"
    type: number
    min: 0
    max: 14
    required: false
```

4. **Workflow-statussen definiëren**:
```yaml
workflow:
  states:
    - code: AANGEVRAAGD
      name: "Aangevraagd"
      color: "#6c757d"
    - code: VOLTOOID
      name: "Voltooid"
      color: "#28a745"
  
  transitions:
    AANGEVRAAGD: [VOLTOOID, GEANNULEERD]
```

5. **AI-strategieën configureren**:
```yaml
ai_strategies:
  ranking: WSJF
  dispatching: MatrixFactorization
  estimating: CategoryLookup
```

---

## GERDA AI-instellingen

Configureer de verschillende modules van het GERDA AI-systeem in `masala_config.json`. Schakel modules in of uit en stel specifieke drempels en gewichten in voor spamdetectie, complexiteitsschatting, rangschikking op basis van WSJF, aanbevelingen voor verzending en capaciteitsvoorspelling.

---

## Seed-data

Gebruik `seed_data.json` om de database te vullen met initiële gegevens voor beheerders, medewerkers, klanten en werkcontainers. Dit is handig voor ontwikkelings- en testdoeleinden.

---

## Voorbeelden

Zie `config/masala_domains.yaml` voor volledige voorbeelden van een IT-support domein en een domein voor tuinonderhoud.

---

## Hot Reload

Wijzigingen in de configuratiebestanden worden automatisch gedetecteerd en opnieuw geladen, tenzij dit expliciet is uitgeschakeld in de globale instellingen.

---

## Beste Praktijken (Best Practices)

1. **Versiebeheer**: Bewaar uw configuratiebestanden in Git.
2. **Commentaar**: Voeg uitleg toe voor bedrijfsspecifieke regels.
3. **Validatie**: Test wijzigingen eerst in een staging-omgeving.
4. **Beveiliging**: Zet nooit wachtwoorden of geheimen in Git (gebruik omgevingsvariabelen).
5. **Modulariteit**: Maak aparte domeinen aan voor verschillende afdelingen.

---

## Verdere Informatie

- [Architectuuroverzicht](../architecture/SUMMARY.md)
- [GERDA-documentatie](../architecture/gerda-ai/GERDA_MODULES.md)
- [API-documentatie](../../api/API_REFERENCE.md)
