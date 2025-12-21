# Tenants vs Domeinen: De Architectuur Begrijpen

**Uitleg over het tweeledige configuratiesysteem in Ticket Masala**

---

## Tenants: Isolatie op organisatieniveau

**Tenant** = **Organisatie/Bedrijf** (bijv. "Overheidsdiensten", "Gezondheidskliniek", "IT-Helpdesk")

### Wat Tenants bieden

Tenants zorgen voor **volledige isolatie** op organisatieniveau:

| Aspect | Beschrijving | Voorbeeld |
|--------|-------------|---------|
| **Data-isolatie** | Aparte database per tenant | `overheid/masala.db` vs `zorg/masala.db` |
| **Configuratie** | Eigen configuratiebestanden | `tenants/overheid/config/masala_domains.yaml` |
| **Branding** | Aangepaste CSS/thema | `tenants/overheid/theme/style.css` |
| **Implementatie** | Aparte Docker-container | Poort 8081 voor overheid, 8088 voor zorg |

### Mapstructuur van een Tenant

```
tenants/
├── overheid/            # Tenant voor gemeentelijke diensten
│   ├── config/
│   │   ├── masala_config.json      # GERDA AI-instellingen
│   │   └── masala_domains.yaml     # Domeindefinities
│   ├── theme/
│   │   └── style.css               # Branding
│   ├── data/
│   │   └── masala.db               # Geïsoleerde database
│
├── zorg/                # Tenant voor medische kliniek
│   └── ...
```

---

## Domeinen: Configuratie van bedrijfsprocessen

**Domein** = **Bedrijfsproces/Workflow** binnen een tenant (bijv. "IT-ondersteuning", "HR", "Financiën")

### Wat Domeinen bieden

Domeinen bieden de **configuratie van de workflow** binnen een tenant:

| Aspect | Beschrijving | Voorbeeld |
|--------|-------------|---------|
| **Workflows** | Statemachines, overgangen | IT: Nieuw → Getrieerd → In behandeling → Klaar |
| **Aangepaste velden** | Domeinspecifieke gegevens | IT: `betrokken_systemen`, `os_versie` |
| **AI-strategieën** | GERDA-configuratie | Verschillende rangschikking per domein |
| **SLAs** | Service Level Agreements | IT-incident: 1 dag, Serviceverzoek: 5 dagen |
| **Entiteitslabels** | Terminologie | IT: "Ticket", HR: "Dossier", Tuinbouw: "Servicebezoek" |

---

## 🔄 Relatie: Tenant → Domein → Ticket

```
Tenant (Organisatie)
  └── Domein (Bedrijfsproces)
       └── Ticket (Werkitem)
```

### Voorbeeldscenario's

#### Scenario 1: SaaS Multi-Tenant
- **Tenant**: "Bedrijf A"
  - **Domein**: "IT Helpdesk"
- **Tenant**: "Bedrijf B"
  - **Domein**: "IT Helpdesk"

**Elke tenant heeft geïsoleerde gegevens, maar kan vergelijkbare domeinen hebben.**

#### Scenario 2: Eén organisatie, meerdere processen
- **Tenant**: "Gemeente"
  - **Domein**: "Burgerzaken"
  - **Domein**: "Bouwvergunningen"
  - **Domein**: "Belastingen"

**Eén tenant met meerdere domeinen die dezelfde database delen.**

---

## 💡 Beslissingshulp

### Wanneer een nieuwe Tenant aanmaken?
- Verschillende organisaties of bedrijven.
- Noodzaak voor volledige data-isolatie.
- Verschillende wet- en regelgeving.
- Apart onderhouds- of implementatieschema.

### Wanneer een nieuw Domein aanmaken?
- Zelfde organisatie, maar een ander bedrijfsproces.
- Noodzaak voor een andere workflow.
- Andere aangepaste velden nodig.
- Gebruikers of gegevens willen delen over processen heen.

---

## 📚 Gerelateerde Documentatie

- [Configuratiegids](CONFIGURATION.md) - Details over domeinconfiguratie
- [Architectuuroverzicht](../architecture/SUMMARY.md) - Algemene architectuur
