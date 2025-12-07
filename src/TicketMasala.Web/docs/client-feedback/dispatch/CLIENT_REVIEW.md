# Client Review: Ticket Masala - Evaluatie voor Brussel Fiscaliteit
**Marc Dubois - Director Project & IT**  
**Brussel Fiscaliteit (Brussels Gewest - Fiscale Administratie)**

---

## 1. Executive Summary

**Datum:** 7 december 2025  
**Reviewer:** Marc Dubois, Director Project & IT  
**Context:** Audit van "Ticket Masala" (Project Dispatch) v3.1 Codebase.  
**Focus:** Is dit "Vaporware" of een echte oplossing?  
**Overall Score:** **8.5 / 10** - **Ready for Pilot (with 1 critical fix)**

### Kernbevindingen

**Strengths (Top 3):**
1.  **De AI Engine (GERDA) is ÉCHT:** Ik heb de code in `Engine/GERDA` gecontroleerd. Dit zijn geen hardcoded if-statements. `MatrixFactorizationDispatchingStrategy` gebruikt daadwerkelijk **ML.NET** om agents te ranken op basis van historie en expertise. Dit is precies wat we nodig hebben.
2.  **Pilot-First Design:** De `ImportController` is geen afterthought. Hij is robuust genoeg voor onze dagelijkse CSV-workflow vanuit SAP. De batch-verwerking is aanwezig.
3.  **Architecturele Flexibiliteit:** De hele applicatie wordt aangestuurd door `masala_config.json`. We kunnen dit uitrollen voor "Hotel Tax" en volgende maand voor "Invordering" zonder een regel code te herschrijven.

**Dealbreakers (Risico's):**
1.  **The "Demo" Gap in Forecasting:** De backend (`AnticipationService.cs`) bevat briljante ML.NET SSA forecasting logica, maar de **Frontend Controller** (`ManagerController.cs`, line 100) gebruikt nog **RANDOM MOCK DATA** voor de grafieken! Dit moet gefixt worden voordat ik dit aan de CFO laat zien.
2.  **Configuratie Veiligheid:** Standaard staan alle AI-modules op `false` in `masala_config.json`. Dit is veilig voor deploy, maar we moeten zeker weten dat dit AAN staat voor de pilot start.

**Aanbeveling:**
**GO voor Pilot (Tax Dept, 63 FTE).** De basis is solide. De AI is geavanceerd. Als de visualisatie van de forecasting gekoppeld wordt aan de *echte* backend data (die er al is), hebben we een winnaar.

---

## 2. Functional Review (G.E.R.D.A. Scan)

Ik heb de codebase gescand op de 5 beloofde pijlers.

### **G - Grouping** (Noise Filter)
*   **Status:** ✅ **Aanwezig**
*   **Implementation:** Geregeld via `ImportController` en background services.
*   **Verdict:** Goed. De batch import workflow dekt onze behoefte om "spam" (dubbele tickets) direct bij de bron aan te pakken.

### **E - Estimating** (The Sizer)
*   **Status:** ✅ **Aanwezig**
*   **Implementation:** `EstimatingService` met `RandomForest` strategieën.
*   **Verdict:** De structuur staat er om complexiteit te voorspellen. Voor de pilot moeten we wel zorgen dat we genoeg trainingsdata hebben, anders valt hij terug op defaults.

### **R - Ranking** (The Prioritizer)
*   **Status:** ✅ **Aanwezig**
*   **Implementation:** WSJF (Weighted Shortest Job First) logica gevonden in `Ranking` folder.
*   **Verdict:** Cruciaal. Dit gaat onze SLA-boetes verminderen doordat urgente tickets eindelijk voorrang krijgen op "makkelijke" tickets.

### **D - Dispatching** (The Matchmaker)
*   **Status:** ⭐ **Uitstekend**
*   **Implementation:** `MatrixFactorizationDispatchingStrategy.cs`.
*   **Code Bewijs:** Ik zie: `_mlContext.Recommendation().Trainers.MatrixFactorization`.
*   **Verdict:** Dit is geen sales-praatje. De code traint daadwerkelijk een model op basis van wie in het verleden succesvol tickets heeft gesloten. Chapeau.

### **A - Anticipation** (The Weather Report)
*   **Status:** ⚠️ **Mixed**
*   **Backend:** ✅ `ForecastBySsa` (Singular Spectrum Analysis) is geïmplementeerd. Zeer geavanceerd.
*   **Frontend:** ❌ `ManagerController` negeert de output en toont `rnd.Next(20, 50)` (willekeurige data).
*   **Verdict:** **FIX THIS.** De backend werkt, maar de manager ziet neppe data. Koppel de View aan de Service output.

---

## 3. Technical Review

### 3.1 Architectuur
Code kwaliteit is hoog. Dependency Injection wordt correct gebruikt. De scheiding tussen `Engine`, `Data`, en `Web` is helder. Dit is maintainable door ons eigen IT-team.

### 3.2 Configuratie
Alles wordt beheerd via `masala_config.json`.
*   **Risico:** In de huidige repo staat `GerdaAI.IsEnabled: true` maar alle sub-modules (Dispatching, Anticipation, etc.) staan op `false`.
*   **Actie:** Voor Pilot Go-Live moet de config worden aangepast.

### 3.3 Data Privacy (AVG/GDPR)
De ML-modellen draaien **Lokaal** (in-process via ML.NET). Er gaat **geen data naar de cloud** (geen OpenAI/Azure calls voor de kernlogica). Dit is een enorm pluspunt voor de juridische goedkeuring.

---

## 4. Gap Analysis & Next Steps

### Critical Fixes (Week 1)
1.  **Connect Forecasting UI:** Verwijder de mock data in `ManagerController.CapacityForecast`. Laat de grafiek de echte `AnticipationService` data tonen.
2.  **Enable Modules:** Update `masala_config.json` om G.E.R.D.A. modules te activeren.

### Pilot (Q1 2026)
*   CSV Import flow is klaar.
*   Training sessies plannen voor de 63 agenten.
*   Configuratie instellen op "Hotel Tax" parameters.

---

## 5. Final Recommendation

**TIER 1: BUY NOW / START PILOT**

Ondanks de (slordige) mock-up data in het dashboard, is de technische fundering van dit project indrukwekkend. Het is zeldzaam om "echte" embedded AI te zien in een .NET applicatie in plaats van dure API-wrappers.

**Advies aan Directie:**
Start de pilot. De ROI op dispatch-efficiëntie (automatisering van 3 uur werk/dag naar 15 min) is direct zichtbaar. De investering van €350k is veilig omdat de code van ons blijft en lokaal draait.

**Signed,**

*Marc Dubois*
*Director Project & IT*
