# Ticket Masala: Systeemoverzicht & Architectuur

Ticket Masala is een volgende generatie **Modulaire Monoliet**, ontworpen om de kloof te overbruggen tussen enterprise ERP-systemen (zoals SAP) en agile, AI-ondersteunde operaties. Dit document biedt een gedetailleerde synthese van de kernmogelijkheden, innovaties en operationele principes van het systeem.

---

## 1. Architecturale Kernpijlers

### Modulaire Monoliet Eerst
Ticket Masala volgt een "Monolith First" aanpak voor eenvoud en prestaties, met behoud van strikte logische grenzen tussen componenten.
- **Implementatie in één Container:** Minimale DevOps-overhead.
- **SQLite Performance Doctrine:** Gebruikmaken van Write-Ahead Logging (WAL) en FTS5 voor lokale data-operaties op hoge snelheid zonder externe database-afhankelijkheden.
- **In-Process Integratie:** Lage latentie communicatie tussen kerndiensten en de AI-engine.

### Multi-Tenant & Multi-Domain
Het systeem ondersteunt een configuratiemodel met twee niveaus:
- **Tenants (Organisatieniveau):** Volledige data-isolatie en branding voor verschillende bedrijven of afdelingen.
- **Domains (Procesniveau):** Unieke workflows (IT, HR, Tuinonderhoud) die dezelfde tenant-infrastructuur delen, maar met verschillende regels, terminologieën en AI-strategieën.

---

## 2. De Configuratie-Engine (DSL Compiler)

Innovatie: **"Compileer, Interpreteer Niet"**

In plaats van trage runtime-logica, gebruikt Ticket Masala een **Domain-Specific Language (DSL)** gebaseerd op YAML die bij het opstarten wordt gecompileerd naar **C# Expression Trees** (`Func<WorkItem, float>`).

- **Dynamische Terminologie:** Verander "Ticket" naar "Incident", "Vergunning" of "Plantverzoek" via configuratie.
- **Stateless Regels:** Bedrijfslogica wordt gedefinieerd als krachtige delegates, wat een executietijd van <1ms garandeert.
- **Hot-Reload:** Configuraties kunnen worden bijgewerkt en herladen zonder de applicatie te herstarten.
- **Versiebeheer:** Elke configuratie-snapshot wordt gehasht (SHA256) en bijgehouden, wat zorgt voor auditeerbaarheid van historische beslissingen.

---

## 3. GERDA AI Dispatch Engine

De **G**rouping, **E**valuation, **R**anking, and **D**ispatch **A**lgorithm (GERDA) is de intelligentiehub van het systeem.

### Belangrijkste AI-componenten:
- **WSJF (Weighted Shortest Job First):** Prioriteert werk op basis van bedrijfswaarde en tijdskriticiteit gedeeld door inspanning.
- **Affinity Routing:** Koppelt terugkerende klanten automatisch aan dezelfde agent voor betere continuïteit.
- **Skill-Based Matching:** Zorgt ervoor dat de juiste agent de juiste complexiteit afhandelt.
- **Explainable AI:** Elke dispatch-aanbeveling bevat een gedetailleerde uitsplitsing (bijv. "+50 Affiniteit", "-20 Werkdruk") zodat teamleads de "waarom" begrijpen.

---

## 4. Privacy & Governance Proxy

Innovatie: **"Het Compliancy-Fort"**

Ticket Masala maakt veilige adoptie van LLM's (zoals OpenAI/Azure) mogelijk door privacy- en kostencontroles te lokaliseren.

- **Lokale PII-Scrubber:** Detecteert en anonimiseert automatisch gevoelige gegevens *lokaal* voordat ze een cloud-API bereiken.
- **Ephemeral AI Pipeline:** Verwerkt documenten (OCR → Samenvatten → Suggereren) in het geheugen; binaire bestanden worden onmiddellijk verwijderd.
- **Budgetbeheer:** Harde en zachte limieten op API-uitgaven per gebruiker en per tenant.

---

## 5. Twitter-Style Kennisbank

Innovatie: **Atomaire Zelf-Rankende Streams**

Een lichtgewicht vervanging voor traditionele statische wiki's, gericht op frictieloze bijdrages.

- **Atomaire Snippets:** Kennisunits ter grootte van een tweet (50-300 woorden).
- **#Hashtag Organisatie:** Geen complexe mappenhiërarchieën; gewoon taggen en zoeken.
- **MasalaRank Algoritme:** Inhoud wordt gerankt op basis van daadwerkelijk gebruik in opgeloste tickets.

---

*Dit document synthetiseert functies en gidsen vanaf december 2025.*
