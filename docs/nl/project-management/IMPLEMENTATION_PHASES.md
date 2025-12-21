# Implementatiefases van Configuratie-uitbreidbaarheid

Dit document houdt de voortgang bij van het project 'Configuratie-uitbreidbaarheid' (`feature/configuration-extensibility`).

## Fase 1: Infrastructuur & Domeinconfiguratie (Voltooid)
**Doel:** Het fundament leggen voor het laden van domeinspecifieke configuraties.
- [x] **Domeinmodel:** Modellen aangemaakt voor `DomainConfig`, `WorkflowConfig`, `AiStrategiesConfig`.
- [x] **Configuratieservice:** `DomainConfigurationService` geïmplementeerd voor het laden van `masala_domains.yaml`.
- [x] **Data-persistentie:** `DomainId` toegevoegd aan de `Ticket`-entiteit.

## Fase 2: Aangepaste velden & Dynamische UI (Voltooid)
**Doel:** Domeinen in staat stellen om eigen gegevensvelden te definiëren die dynamisch worden weergegeven.
- [x] **Validatieservice:** `ICustomFieldValidationService` aangemaakt voor typesafety.
- [x] **Dynamische weergave:** Partial views geïmplementeerd voor het aanmaken, bewerken en weergeven van velden.
- [x] **Opslag:** Gebruik van JSON (`CustomFieldsJson`) voor de opslag van aangepaste gegevens.

## Fase 3: Workflow Engine (Voltooid)
**Doel:** Domeinspecifieke statustransities en bedrijfsregels afdwingen.
- [x] **Rule Engine:** `IRuleEngineService` geïmplementeerd voor validatie van overgangen.
- [x] **Logica-afdwinging:** Integratie in de `TicketService`.
- [x] **UI-filtering:** De status-dropdown in de bewerkingsweergave filteren op basis van geldige vervolgstappen.

## Fase 4: AI-strategie Uitbreiding (Voltooid)
**Doel:** Domeinen de keuze geven welke GERDA AI-strategieën worden gebruikt.
- [x] **Strategy Factory:** Factory om strategieën zoals `IJobRankingStrategy` op naam op te lossen.
- [x] **Integratie:** Services gebruiken de ingestelde strategie uit de domeinconfiguratie.

## Fase 5: Prestatie-optimalisatie (Rule Compiler) (Voltooid)
**Doel:** De Rule Engine versnellen door over te stappen van runtime-interpretatie naar gecompileerde expressiebomen.
- [x] **Rule Compiler Service:** Implementatie met `System.Linq.Expressions`.
- [x] **Caching:** Gecompileerde regels cachen bij het opstarten of herladen.

## Fase 7: UI-lokalisatie & Branding (In behandeling)
**Doel:** Domeinspecifieke labels en thema's bieden.
- [ ] **Lokalisatieservice:** Hardcoded labels (zoals "Ticket") vervangen door configuratie-lookups.
- [ ] **Domeinwisselaar:** Domein-switcher toevoegen voor implementaties met meerdere domeinen.

## Fase 8: Schaalbare Ingestie (Gatekeeper) (In behandeling)
**Doel:** Ontkoppelde verwerking voor hoge doorvoer (bijv. IoT of webhooks).
- [ ] **Gatekeeper API:** Aparte Minimal API voor webhooks.
- [ ] **Message Bus:** Implementatie van een eenvoudige wachtrij (`System.Threading.Channels`).

---
**Legenda:**
- Voltooid
- In behandeling / Volgende
- Niet gestart
