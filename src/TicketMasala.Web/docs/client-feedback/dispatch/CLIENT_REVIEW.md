# Client Review: Ticket Masala - Evaluatie voor Brussel Fiscaliteit
**Marc Dubois - Director Project & IT**  
**Brussel Fiscaliteit (Brussels Gewest - Fiscale Administratie)**

---

## 1. Executive Summary

**Datum:** 5 december 2025  
**Reviewer:** Marc Dubois, Director Project & IT  
**Organisatie:** Brussel Fiscaliteit - 382 FTE, €1.2B revenue managed, 85k cases/year  
**Context:** €350k pilot budget (63 FTE Tax dept), zoekt intelligent dispatch layer BOVENOP SAP/Qlik  
**Overall Score:** **9.0 / 10** - **Ready for Pilot**

### Kernbevindingen

**Strengths (Top 3):**
1.  **GERDA AI Intelligence (9.5/10):** De 4-factor affinity scoring en WSJF prioritization zijn precies wat we missen in SAP. Het systeem 'begrijpt' wie welk dossier moet krijgen. De forecasting module (ML.NET) is technisch indrukwekkend.
2.  **Modern Collaboration (9/10):** De implementatie van document management (preview/upload), rich-text chat en @mentions is solide. Dit elimineert de noodzaak voor schaduw-IT tools zoals WhatsApp.
3.  **Architecture & Maintainability (9/10):** Clean code, Repository pattern, D.I., en configuratie-driven opzet. Dit is geen "spaghetti code" prototype, maar een professioneel fundament dat wij intern kunnen beheren.

**Dealbreakers (Risico's):**
1.  **Forecasting UI:** De AI backend voor forecasting is briljant, maar de frontend visualisatie (grafieken, alerts) voor managers is nog minimaal.
2.  **Enterprise Integrations:** Voor de pilot is de CSV import tool voldoende (en werkt goed), maar voor een organisatiebrede uitrol (382 FTE) is de ontbrekende directe API-koppeling met SCASEPS/Exchange een blocker.
3.  **Schaalbaarheid:** Onbewezen met 50k+ tickets. Load testing is vereist voor fase 2.

**Aanbeveling:**
**GO voor Pilot (Tax Dept, 63 FTE).** De applicatie is matuur genoeg voor de pilot in Q1 2026. De "must-haves" (docs, chat, search, CSV import) zijn aanwezig en werken. De ROI potentie (vooral door overtime reductie) rechtvaardigt de investering volledig.

---

## 2. Functional Review

### 2.1 Intelligent Dispatching (Must-Have)
**Score: 9.5/10**
De GERDA implementatie is de "killer feature" van dit platform.
-   **Affinity Scoring:** Werkt uitstekend. Het systeem kijkt naar historie en expertise, niet alleen naar "wie heeft tijd".
-   **Batch Operations:** De `ImportController` en batch actions in de UI maken het mogelijk om grote volumes te verwerken, wat essentieel is voor onze "Monday morning" pieken.
-   **Spam Detection:** Clustering werkt en voorkomt vervuiling van de backlog.

### 2.2 Workload Visibility & Forecasting (Must-Have)
**Score: 7.5/10**
-   **Backend:** `AnticipationService.cs` met ML.NET Time Series SSA is state-of-the-art. Het kan capaciteitsrisico's detecteren.
-   **Frontend:** Hier laat het steken vallen. Ik zie nog geen interactieve grafieken of "scenario planners" in het dashboard. De data is er, maar de manager kan er niet mee "spelen".
-   **Dashboard:** Het `TeamDashboard` is wel nuttig en toont real-time metrics, wat een enorme verbetering is t.o.v. SAP rapportage.

### 2.3 Collaboration & Context (Must-Have)
**Score: 9/10**
-   **Chat:** Rich text en internal/external notes toggle werken perfect.
-   **Document Management:** `TicketController` ondersteunt nu upload en preview. Dit was een kritieke eis en is goed opgelost.
-   **Context:** Alles zit in één dossier. Geen e-mails meer die los zweven van het SAP dossier.

### 2.4 Quality & Compliance (Must-Have)
**Score: 8.5/10**
-   **Review Workflow:** Aanwezig en functioneel. Juniors kunnen review aanvragen, seniors kunnen scoren.
-   **Audit Trail:** Basic logging is aanwezig via `AuditService`. Voor volledige GDPR compliance (wie bekeek wat) is mogelijk nog meer detail nodig in de logs, maar de basis staat.

### 2.5 Search & Filter (Must-Have)
**Score: 9/10**
-   Functioneel compleet. Saved filters, text search, en dynamische dropdowns voor status/agent werken snel en intuïtief.

---

## 3. Technical Review

### 3.1 Architectuur Kwaliteit
**Oordeel:** Uitstekend.
De codebase gebruikt moderne patterns (Repository, Observer voor GERDA). Dit betekent dat we componenten kunnen vervangen zonder alles te breken. De keuze voor **ML.NET** is slim: data blijft lokaal (GDPR!) en geen dure cloud API calls. Configuratie via `masala_config.json` geeft ons de flexibiliteit die we zochten zonder telkens developers te bellen.

### 3.2 Scalability & Security
**Oordeel:** Voldoende voor Pilot, aandachtspunt voor Rollout.
-   **Security:** Role-based access (RBAC) is goed geïmplementeerd. Input validation is overal aanwezig.
-   **Scalability:** De database structuur is degelijk, maar met 85k cases/jaar zal de database groeien. Er is nog geen caching stick (Redis) of geavanceerde indexing strategie zichtbaar voor archief data.

### 3.3 Data Import (Pilot Focus)
**Oordeel:** Goed.
De `ImportController` bevat logica om CSV files te parsen, mappen en valideren. Dit is cruciaal voor onze pilot waar we afhankelijk zijn van SCASEPS exports. De error handling bij import is aanwezig.

---

## 4. User Experience Review

### 4.1 Agent Perspectief
Een verademing vergeleken met SAP.
-   **Snelheid:** Navigatie is direct. Geen wachttijden of complexe menu structuren.
-   **Duidelijkheid:** Status bars, color coding voor prioriteit, en duidelijke actieknoppen.
-   **Mobile:** De interface is responsive (Bootstrap), wat betekent dat agents op tablets kunnen werken (groot pluspunt voor thuiswerk).

### 4.2 Manager Perspectief
Eindelijk grip op de zaak.
-   Het dashboard geeft direct inzicht in SLA status en workload.
-   Batch assign functionaliteit bespaart uren dispatch werk per dag.
-   Kwaliteitsreviews zijn geïntegreerd, niet meer via mail/Excel.

---

## 5. Gap Analysis

### Missing Must-Haves (0-3 months)
*Geen kritieke functionele gaten meer gevonden voor de PILOT fase.*
De document management en import tools die eerder ontbraken zijn nu aanwezig.

### Missing Nice-to-Haves (3-12 months)
1.  **Visuele Forecasting:** Grafieken voor de voorspellingen.
2.  **API Integraties:** Directe koppeling SCASEPS/Qlik (nodig voor na de pilot).
3.  **Advanced Audit UI:** Een interface om logs te doorzoeken (nu alleen database/backend).

---

## 6. Pilot Feasibility

**Conclusie:** **JA, we kunnen starten.**
De applicatie voldoet aan de eisen voor de Tax Department pilot (63 FTE).
-   **Data:** We kunnen historische data inladen via CSV.
-   **Proces:** De dagelijkse CSV export/import workflow is werkbaar voor de duur van de pilot.
-   **Risico:** Laag. We draaien in "shadow mode" naast SAP, maar de productiviteitswinst zal snel duidelijk zijn.

---

## 7. Cost-Benefit Analysis

**Kosten:**
-   **Implementatie (Pilot):** €350k (Reeds gebudgetteerd).
-   **Licenties/Cloud:** Minimaal (draait op eigen infra of goedkope cloud containers).

**Baten (Geschat):**
-   **Overtime Reductie:** €220k/jaar (door efficiëntere dispatch en minder crisis management).
-   **SLA Boetes:** €340k/jaar (vermeden door betere prioritering).
-   **Consultant Saving:** €600k/jaar (minder afhankelijkheid van SAP wijzigingen).

**ROI:**
Zelfs in het meest conservatieve scenario is de terugverdientijd minder dan 6 maanden na volledige uitrol.

---

## 8. Final Recommendation

**TIER 1: BUY NOW / START PILOT**

Ticket Masala heeft bewezen geen "vaporware" te zijn. De code is solide, de AI functionaliteit is oprecht innovatief en nuttig (geen gimmick), en de kritieke features voor overheidswerk (documenten, audit, security) zijn aanwezig.

**Advies aan Directie:**
Keur de start van de pilot per **1 januari 2026** goed. Dit is de beste kans die we hebben om onze operationele efficiëntie te moderniseren en de "brain drain" van pensionering op te vangen met GERDA's knowledge capture.

**Signed,**

*Marc Dubois*
*Director Project & IT*
