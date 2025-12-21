# Ticket Masala v3.1+ - Toekomstige Roadmap

**Versie:** 3.1, 3.2, 4.0  
**Datum:** December 2025  
**Status:** Planning

---

## Overzicht

Dit document beschrijft de verbeteringen die zijn gepland voor **versie 3.1 en verder**. Deze bouwen voort op het v3.0 MVP-fundament en bevatten functies die meer inspanning vereisen.

---

## v3.1 - Configuratie & Waarneembaarheid

### 1.1 Scriban-sjablonen voor Ingestie
**Probleem:** Ingestie-mappings vereisen momenteel codewijzigingen.  
**Oplossing:** Introductie van de `IngestionTemplateService` die gebruikmaakt van Scriban-sjablonen in `appsettings.json`.  
**Status:** Geïmplementeerd.

### 1.2 Configuratie Hot Reload
**Probleem:** Wijzigingen in de configuratie vereisen een herstart van de applicatie.  
**Oplossing:** Gebruik van `FileSystemWatcher` voor automatische herlaadacties.  
**Status:** Geïmplementeerd in `DomainConfigurationService.cs`.

### 1.3 Schema-validatie voor CustomFieldsJson
**Probleem:** Geen validatie voor dynamische JSON-velden.  
**Oplossing:** `CustomFieldValidationService` valideert typen, verplichte velden, min/max en selectie-opties.  
**Status:** Geïmplementeerd.

### 1.4 Prometheus Metrics Export
**Probleem:** Geen inzicht in de prestaties van GERDA.  
**Oplossing:** Een eenvoudig `/metrics` endpoint met statistieken over uptime, geheugengebruik en garbage collection.  
**Status:** Geïmplementeerd.

---

## v3.2 - AI & Plugin-systeem

### 2.1 Uitlegbaarheids-API (Explainability)
**Probleem:** Gebruikers begrijpen de aanbevelingen van GERDA niet altijd.  
**Oplossing:** Geef bij elke aanbeveling de factoren mee die hebben bijgedragen aan de beslissing.

### 2.2 Leercyclus via Feedback
**Probleem:** GERDA kan niet leren van correcties door gebruikers.  
**Oplossing:** Registreer of aanbevelingen worden geaccepteerd of afgewezen om het model te verbeteren.

### 2.3 Plugin-architectuur
**Probleem:** Het toevoegen van nieuwe AI-strategieën vereist codewijzigingen.  
**Oplossing:** Ondersteuning voor het laden van plugins tijdens runtime vanuit een `/plugins` map.

---

## v4.0 - Enterprise Functionaliteiten

### 4.1 Event Sourcing (Optioneel)
Biedt een volledig auditverleden en de mogelijkheid om gebeurtenissen opnieuw af te spelen. Alleen te overwegen bij zeer strikte audit-eisen.

### 4.2 NLP-samenvattingen (Lokaal LLM)
Gebruik van een lokaal taalmodel (zoals Phi-3 of Llama) om lange ticketbeschrijvingen automatisch samen te vatten voor een snellere triage.

---

## Prioriteitenmatrix

| Item | Versie | Inspanning | Prioriteit |
|------|---------|--------|----------|
| Scriban-sjablonen | v3.1 | Gemiddeld | Hoog |
| Uitlegbaarheids-API | v3.2 | Hoog | Hoog |
| Hot Reload Configuratie | v3.1 | Laag | Gemiddeld |
| Prometheus Statistieken | v3.1 | Laag | Gemiddeld |
| Plugin-architectuur | v3.2 | Gemiddeld | Gemiddeld |
| NLP-samenvattingen | v4.0 | Hoog | Toekomst |
