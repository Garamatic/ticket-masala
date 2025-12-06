# Client Feedback Persona - Marc Dubois, Director Project & IT

## Achtergrond

**Naam:** Marc Dubois  
**Functie:** Director Project & IT  
**Organisatie:** Brussel Fiscaliteit (Brussels Gewest - Fiscale Administratie)  
**Directie:** Projecten en IT (1 van 11 directoraten)  
**Locatie:** Brussel, België  
**Rapporteert aan:** Vice Director-General (onder Director-General)

**Organisatie Structuur:**
- **Total headcount:** 382 medewerkers
- **Leadership:** 1 Director-General, 1 Vice DG, 11 Directors (waaronder ik)
- **My department (Project & IT):** 28 FTE
- **Case-facing departments:** 179 case agents, 98 team leads, 89 coordinators

**Departments Overview:**
- Client Management: 83 FTE
- Financial: 65 FTE  
- Real Estate Expertise: 14 FTE
- Legal & Appeals: 35 FTE
- Tax Department: 63 FTE
- Fiscal Centre: 10 FTE
- HR: 29 FTE
- Data Management: 33 FTE
- **Project & IT: 28 FTE** ← my team

**Scale & Impact:**
- **Revenue managed:** €1.2 billion/year
- **Tax types:** 15 different tax categories (VAT, property tax, vehicle tax, etc.)
- **Cases per year:** 327 040
- **Case channels:** Email (26%), MyTax portal (20%), Phone (15%) call center (11%) internal (10%) post (13%) crm (5%) guichet and others (rest) 

**Ervaring:** 15 jaar in publieke sector IT, voormalig SAP consultant (ironisch genoeg)

## Huidige Situatie - Pijnpunten

### Legacy Stack - The €7M/Year Monster

**Current IT Landscape (we're NOT replacing, need to integrate with):**
- **SAP ECC 6.0** (sinds 2008): Core financial system, tax calculations, internal workflows
  - Status: **Keeping** - too expensive/risky to replace, already paid for
  - Integration need: Export PDFs (tax assessments, decisions, official docs)
  
- **SAP CRM / SCASEPS** (sinds 2012): Case intake system (email/post → tickets)
  - Status: **Keeping** - case creation happens here (email/post/phone)
  - Integration need: Bi-directional sync - SCASEPS creates case → Ticket Masala for dispatch/workflow → status back to SCASEPS
  - Pain: We don't want to REPLACE this (too integrated with SAP), we want to AUGMENT it with intelligent dispatch
  
- **Qlik Sense** (sinds 2021): BI dashboards, reporting, analytics
  - Status: **Keeping** - executives love it, already paid €60k setup
  - Integration need: Pull client data (customer history, financials, risk profiles) to enrich case assignment
  - Use case: Qlik has the "who" (client profile), Ticket Masala decides "which agent"
  
- **MyTax Portal** (custom .NET, sinds 2018): Citizen self-service (35% of case intake)
  - Status: **Keeping** - citizens use it, works fine
  - Integration need: API for case status updates (real-time instead of 24h delay)
  
- **SharePoint** (2016): Document management
  - Status: **Might replace** - nobody uses it properly, chaos
  - Question: Can Ticket Masala handle PDF storage/preview or do we keep SharePoint?

**Total Annual Cost: €7,000,000**
- SAP licenses: €1,200,000
- SAP maintenance & support: €850,000
- SAP consultants (average): €2,100,000 (for changes, upgrades, "firefighting")
- Infrastructure (on-prem + Azure hybrid): €1,400,000
- Qlik + other tools: €450,000
- Internal IT staff cost allocated to legacy maintenance: €1,000,000

**What We're Actually Looking For:**
- **NOT a SAP replacement** - we're stuck with SAP, it's the "system of record"
- **NOT a Qlik replacement** - executives are happy with dashboards
- **YES to intelligent dispatch layer** - sits ON TOP of existing systems
- **YES to agent productivity tool** - better UX than SAP for daily work
- **YES to integration** - bridge between SAP/SCASEPS (intake) + Qlik (analytics) + agents (execution)

**Integration Requirements:**

**PILOT PHASE (Manual CSV - No API Approvals Yet):**

1. **SCASEPS Data Import (Daily Manual Process)**
   - Format: CSV export from SCASEPS (daily batch, ~150-200 new cases/day for Tax dept)
   - Fields: Case ID, Client ID, Case Type, Priority, Status, Creation Date, Description, Assigned Agent
   - Process: Team lead exports CSV from SCASEPS (8:00 AM) → uploads to Ticket Masala (8:15 AM)
   - Time: ~15 minutes/day (acceptable for pilot, not scalable for 382 FTE)
   - Two-way sync: Export assignments from Ticket Masala → manual CSV upload back to SCASEPS (end of day)

2. **Qlik Sense Data Pull (Weekly Manual Process)**
   - Format: CSV exports for client enrichment data
   - Data: Client ID, Risk Profile, Payment History, Open Cases Count, Total Outstanding Amount
   - Process: Data analyst exports Qlik data → uploads to Ticket Masala (Monday mornings)
   - Use case: AI dispatch sees enriched client profile (even if 1 week stale, better than nothing)
   - Frequency: Weekly acceptable for pilot (client data doesn't change daily)

3. **Historical Data Import (One-Time)**
   - Qlik historical export: 3 years of closed cases (CSV, ~60k rows for Tax dept)
   - SCASEPS case archive: Past case resolutions, decision patterns
   - Purpose: Train GERDA AI models on historical patterns before pilot go-live
   - Timeline: Import during setup phase (week 2-3 of Q1 2026)

**ROLLOUT PHASE (Real APIs - Pending Security Approval):**

4. **SCASEPS API Integration (Must-Have for Rollout)**
   - Case creation trigger: SCASEPS webhook → real-time import to Ticket Masala
   - Bi-directional sync: Ticket Masala assignment/status → push back to SCASEPS (audit trail)
   - Document linking: SCASEPS PDF attachments → direct access (no re-upload)
   - Security approval: 3-6 months process (start during pilot evaluation June 2026)

5. **Qlik Sense Integration (Nice-to-Have for Rollout)**
   - Real-time client data pull via REST API
   - Use case: AI dispatch with fresh client risk profiles
   - Fallback: If not approved, keep weekly CSV process (works but not optimal)

6. **SAP ECC Integration (Future, Post-Rollout)**
   - Export official documents: Tax assessments, signed decisions (PDF generation)
   - Financial data: Outstanding amounts, payment plans
   - HR sync: Agent vacation calendar (for capacity forecasting)

**The Reality Check:**
- SAP will NEVER go away (too expensive, too risky, political suicide)
- Qlik executives love it (pretty charts for Minister presentations)
- SCASEPS is the legal "case of record" (audit trail, official case numbers)
- **Ticket Masala's job:** Make agents 10x more productive WITHOUT ripping out legacy systems
- **Integration strategy:** Thin layer on top, not a monolithic replacement

### Concrete Operationele Problemen

**1. Werkdistributie Chaos (daily nightmare)**

**Case Intake Channels:**
- Email: 45% (~38,000 cases/year) → SAP CRM tickets (manual review needed)
- MyTax Portal: 35% (~30,000 cases/year) → auto-created in SAP CRM
- Post: 15% (~13,000 cases/year) → scanned → manual SAP entry (2-3 day delay)
- Phone: 5% (~4,000 cases/year) → agent creates SAP ticket during call

**The Dispatch Nightmare:**
- **98 team leads** manually assign cases to **179 agents** daily
- Time spent on dispatch: **2.5-4 hours/day per team lead** (collective waste: 245-392 hours/day!)
- Method: Excel spreadsheets + email + "tribal knowledge"
- Load balancing: Non-existent
  - Example: Agent Lisa (junior, VAT team): 3 cases assigned
  - Example: Agent Piet (senior, VAT team): 41 cases assigned (burned out, considering sick leave)
- Cherry picking: Rampant (agents close easy cases fast to "look good", complex cases rot)

**Current Performance Metrics (2024 YTD):**
- **SLA breach rate: 23%** (target: <8%, contractual obligation to Brussels region)
- Average case closure time: 18 days (target: 12 days)
- Cases >30 days old: 4,200 (12% of active backlog)
- Cases >60 days old: 890 (red flag, legal exposure)
- Citizen complaints: +34% vs. 2023
- Agent burnout (sick leave >10 days): 47 agents (26% of workforce!)

**Cost of SLA Failures:**
- Penalties paid to Brussels region: €340,000 in 2024 (contractual SLA penalties)
- Legal appeals (citizen dissatisfaction): €180,000 in legal dept costs
- Overtime to catch up: €220,000
- **Total cost of poor dispatch: ~€740,000/year**

**2. Onzichtbare Werklast & Forecasting Black Hole**

**Current Capacity Planning Process:**
- Director asks (in October): "How many FTE do we need for Q1 2026 (tax season)?"
- My answer: "Uh... let me check last year... +10%? Maybe 15 temps?"
- CFO asks: "Show me the data and assumptions"
- My reality: "I have Excel with last 3 years' case counts, but complexity varies wildly"

**What We DON'T Know:**
- Case complexity distribution (simple address change = 10 min, tax audit appeal = 40 hours)
- Seasonal patterns (property tax = spike in May, VAT = steady, vehicle tax = January spike)
- Agent velocity by type (Senior Marie closes 2 complex/day, Junior Tom closes 8 simple/day)
- Impact of policy changes (new tax law = unpredictable spike, e.g., COVID rent support = +12k cases in 2 weeks)
- Real capacity (agents work 60% on cases, 40% on meetings/admin/training)

**Consequences:**
- **Hiring decisions: 3-4 months too late** (by the time we see the spike, it's crisis mode)
- Budget battles: "We need 20 temps for Q1" - CFO: "Prove it" - Me: *shows Qlik chart* - CFO: "That's last year, I want a FORECAST"
- Temporary staff: Scramble to hire via agencies (€50/hour vs. €35 internal cost = waste)
- Overtime spiral: Can't hire fast enough → agents work overtime (expensive + burnout)

**2024 Example - The May Disaster:**
- Property tax reform announced (April 15)
- Expected case increase: +30% (my "gut feeling")
- Actual increase: +340% (May-July)
- Response: Hired 12 temps in June (6 weeks too late)
- Overtime cost: €180k
- SLA breaches: 890 cases
- Agent sick leave: 11 agents burned out in Q2

**What Qlik Shows (but can't help with):**
- Pretty line chart: "Cases per month 2021-2024" (I can SEE the seasonal pattern)
- Bar chart: "Avg closure time by department" (I can SEE Legal is slow)
- **What it DOESN'T do:** Predict next month, recommend staffing, simulate "what if" scenarios

**The €2M Question:**
If I could forecast accurately, I'd:
- Hire temps 6 weeks BEFORE spike (not during crisis)
- Save €300k/year in overtime
- Avoid €340k SLA penalties
- Prevent 15-20 agents from burning out
- Reduce citizen complaints by 40%
- **ROI: ~€2M/year in cost avoidance + satisfaction gains**

**3. Institutional Knowledge Lock-in (the "Sophie Problem")**

**The Scenario:**
- Agent Sophie Vermeulen (28 years at Brussel Fiscaliteit, 58 years old, retiring 2027)
- Expertise: Property tax appeals for Brussels-Capital region (14/19 municipalities)
- Knowledge: 28 years of case law, edge cases, unwritten rules, contact network
- Cases handled: ~4,200 property tax appeals (average 150/year)
- Replacement plan: None (we're screwed)

**What Happens When Sophie Takes Vacation (3 weeks, August 2024):**
- Day 1: 12 new property tax appeals arrive
- Day 2: Team lead assigns to Agent Tom (2 years experience, mainly VAT background)
- Day 3: Tom struggles, calls Legal dept: "How do I handle Brussels-specific règlement?"
- Day 4: Tom finds old case file (paper archive, takes 3 hours to locate)
- Day 5: Tom makes decision (probably wrong, we'll find out when citizen appeals)
- Week 2: Backlog grows to 47 cases waiting for Sophie
- Week 3: Sophie returns → 47 cases + 18 urgent escalations = works overtime for 2 weeks

**The Knowledge Gap:**
- **89 coordinators** (middle management) each have specialized knowledge
- **98 team leads** know "their" agents, "their" case types, "their" edge cases
- **179 agents** - 40% have >10 years tenure (institutional memory)
- Knowledge capture: ZERO (it's all in people's heads)
- Onboarding new agents: 6-9 months to be "useful", 2 years to be "good"
- Knowledge transfer: "Sit next to Sophie for 3 months, good luck"

**Real Cost:**
- Sophie-type experts: ~25 across organization (retiring wave 2025-2030)
- Replacement cost: 2 juniors per 1 expert (double headcount to maintain capacity)
- Training time: 2 years × €45k salary = €90k per replacement
- Lost efficiency: Juniors are 40% slower for first 3 years
- Error rate: 3x higher for juniors (costly appeals, reputation damage)

**What We Need (but don't have):**
- Case similarity search: "Show me 10 similar property tax appeals Sophie handled"
- Decision reasoning: "Why did Sophie decide X? What was the precedent?"
- Knowledge base: "For case type Y, here are the 5 common solutions (with success rates)"
- Automatic routing: "This case is 85% similar to Sophie's specialty → route to Sophie (or her successor)"

**The 2027 Retirement Wave:**
- 34 agents retiring 2025-2028 (average tenure: 24 years)
- Estimated knowledge loss: Equivalent to 68 junior FTE
- Replacement budget: €6.8M over 3 years (salaries only, not counting productivity loss)
- Risk: Service quality collapse, SLA breaches spike, citizen satisfaction tanks

**4. Kwaliteitscontrole = Niet-bestaand**
- Junior agents maken fouten → ontdekken we pas als burger belt (3 weken later)
- Geen peer review proces
- Geen feedback loops
- Performance reviews: 1x per jaar, based on manager's "gevoel", zeer subjectief
- Gevolg: Juridische fouten, burgers die naar Raad van State stappen, reputatieschade

**5. Communicatie Apocalyps**
- Agents gebruiken: WhatsApp groepen (GDPR nightmare), persoonlijke email, telefoon, Post-its
- Teamleider vraagt: "Waarom besloot je dit?" - Agent: "Uh, dat besprak ik met collega's in WhatsApp?"
- Audit trail: non-existent
- Case overdracht: 40 minuten face-to-face uitleg (of bij remote work: "lees maar het dossier, succes")

## Wat Ik Zoek - Specifieke Requirements

### Must-Haves (dealbreakers als dit er niet is)

**1. Intelligent Dispatching**
- Niet "first-come-first-served" maar "right work to right person"
- Factoren: agent expertise, historical performance, current workload, case urgency
- Auto-suggest: "Case X best toewijzen aan Agent Y because Z"
- Manager override mogelijk (AI suggereert, mens beslist)
- Batch operations: 50 cases selecteren → "auto-dispatch" → done in 30 seconden

**2. Workload Visibility & Forecasting**
- Real-time dashboard: wie heeft hoeveel cases (+ complexity weight, niet alleen count)
- Predictive: "Op basis van trend verwacht ik 340 nieuwe cases volgende maand"
- Capacity planning: "Je hebt 8 FTE, forecast is 400 cases, dat is 120% load → hire 2 extra temps"
- Scenario planning: "What if 3 agents ziek zijn? Where's the bottleneck?"

**3. Collaboration & Context**
- Inline chat per case (Teams-achtig, maar geïntegreerd in het dossier)
- @mentions om collega's te taggen
- Rich text + file attachments
- Internal notes vs. external communication (burger mag niet alles zien)
- History: "Agent X vroeg dit, Agent Y antwoordde dit, beslissing werd Z omdat ..."

**4. Quality & Compliance**
- Audit trail: wie deed wat, wanneer, waarom
- Review workflow: Junior completes → Senior reviews → Approved/Send back
- Checklists/templates: "Voor type X case, dit zijn de verplichte stappen"
- Knowledge base linking: "10 similar cases, 8 werden opgelost met oplossing A"

**5. Search & Filter (non-negotiable)**
- Full-text: zoek in case description, comments, attachments
- Advanced filters: status + agent + date range + client type + region + ...
- Saved queries: "Mijn urgent cases", "Team backlog > 30 dagen"
- Export: Excel/PDF voor rapportage aan directie

### Nice-to-Haves (very strong preference)

**For Pilot (Manual CSV approach):**
- **CSV Import Tool:** Drag-drop upload, field mapping wizard, validation reports
- **Document management:** Upload/preview PDF (or link to SCASEPS PDF storage)
- **Email integration:** Notifications only (case creation stays in SCASEPS)
- **Mobile app:** Agents kunnen cases bekijken/updaten op tablet (hybrid work)
- **SLA automation:** Auto-escalate als case 80% van deadline bereikt
- **Customizable dashboards:** Elke teamleider wil eigen KPIs zien

**For Rollout (API-based, post-pilot):**
- **SCASEPS API integration:** Bi-directional sync (webhook case creation + status push-back)
- **Qlik API integration:** Pull client context data to enrich dispatch decisions
- **SAP document export:** Generate/retrieve official PDFs (future phase)
- **SSO/SAML:** Single sign-on with existing Active Directory

### Deal-Sweeteners (would be amazing)

- **AI case summarization:** "Geef me 3-line summary van dit 40-page dossier"
- **Suggested responses:** "Based on 200 similar cases, hier zijn 3 template antwoorden"
- **Anomaly detection:** "Case X lijkt verdacht, mogelijk fraude, flag for review"
- **Multilingual support:** NL/FR/EN (België, you know)

## Evaluatie Criteria

### Must Score 8+/10 Op:

1. **Dispatching Intelligence** - Can it actually save us 2-3 hours/day on manual assignment?
2. **Forecasting Accuracy** - Does the AI prediction help hiring decisions or is it voodoo?
3. **User Adoption** - Will our 55-year-old agents use this or rebel and demand SAP back?
4. **Time-to-Value** - Pilot in 3 months or 12 months?

### Must Score 6+/10 Op:

5. **Search/Filter Power** - Can I find case X in 10 seconds or 10 minutes?
6. **Collaboration Tools** - Can teams actually work together or still need WhatsApp?
7. **Mobile Experience** - Usable on iPad or "desktop only sorry"?
8. **Customization** - Config file changes or "need a developer"?

### Nice if 5+/10:

9. **Document Management** - Good enough for PDFs or integrate with SCASEPS docs?
10. **Reporting Flexibility** - Complements Qlik (not replaces - execs won't allow it)
11. **Integration Options** - SCASEPS API? Qlik REST API? SAP exports? Or locked silo?
12. **Security/Compliance** - GDPR audit-proof or "we'll add that later"?

## Budget & Timeline Context

**Current Annual IT Costs (Legacy Stack):**
- SAP ECC + CRM licenses: €1,200,000
- SAP maintenance & support: €850,000  
- SAP consultants (annual average): €2,100,000
- Infrastructure (on-prem + cloud): €1,400,000
- Qlik Sense: €80,000
- Other tools (SharePoint, etc.): €370,000
- **Total Legacy IT Budget: €7,000,000/year**

**Hidden Costs (not in IT budget, but real):**
- SLA penalties (paid by Operations): €340,000/year
- Overtime (poor dispatch): €220,000/year
- Legal dept (appeals from errors): €180,000/year
- Productivity loss (manual work): ~€1,200,000/year (estimated 2.5 FTE equivalent wasted daily on dispatch)
- **Total True Cost: ~€8,940,000/year**

**Available Budget for New Solution:**

**Phase 1 - Pilot (Approved):**
- One-time project budget: **€350,000** (approved by DG for "operational excellence initiative")
- Pilot scope: 1 department (Tax dept: 63 FTE, 7 team leads, 2 coordinators)
- Timeline: 6 months (Jan-June 2026)
- **Integration approach: MANUAL (no API connections approved yet)**
  - Historical data: Qlik Sense CSV exports (client history, case patterns)
  - Real-time cases: SAP/SCASEPS CSV exports (daily batch import)
  - Rationale: IT security approval for API connections = 3-6 months, pilot can't wait
  - Acceptable: Manual import burden for 63 users, proves value before requesting prod integrations

**Phase 2 - Rollout (Conditional):**
- Budget depends on pilot ROI proof
- If pilot saves >€500k/year → full budget approval likely
- Estimate needed: **€800k - €1.2M** (for full 382 FTE rollout)
  - **€200k-€300k for API integrations** (SCASEPS bi-directional, Qlik connector, SSO)
  - €500k-€800k for platform scaling, training, change management
  - IT security approval process: 3-6 months (start during pilot evaluation)
- Target: Reduce SAP consultant dependency (€2.1M → €1.5M = save €600k/year)
- Reality: We won't REPLACE SAP CRM, but augment it (agents work in Ticket Masala, not SAP UI)

**Phase 3 - Operational (Annual):**
- Can allocate up to €250k/year for new system (net new budget)
- Expected savings: €600k-€900k/year (reduced SAP consultants + overtime + SLA penalties)
- ROI target: 2.5x return (€250k cost → €625k+ savings)
- Integration costs: Budget €100k/year for SCASEPS/Qlik API maintenance

**Timeline Expectations:**

**Q4 2025 (Now):**
- Vendor evaluation: December 2025
- Decision: Late December / Early January
- Contract negotiation: January 2026

**Q1 2026 - Pilot Prep:**
- Setup & configuration: 4 weeks
- **Data migration (manual CSV process):** 2 weeks
  - Historical: Export 3 years Qlik data → CSV → import to Ticket Masala (one-time)
  - Real-time: Setup daily SAP/SCASEPS CSV export job (automated on SAP side)
  - Client data: Qlik CSV exports for customer profiles, case history
- User training (63 users): 2 weeks
- Pilot go-live: March 1, 2026

**Q2 2026 - Pilot Execution:**
- Live operations: March - May (3 months)
- **Data refresh:** Daily CSV import from SAP (new cases, status updates)
  - Process: Team lead runs CSV export from SCASEPS → uploads to Ticket Masala (15 min/day)
  - Acceptable burden for pilot, not sustainable for full rollout
- Monitoring & tweaking: Ongoing
- User feedback collection: Weekly
- **Integration feasibility study:** Parallel workstream (prepare API specs for Phase 2)
- Pilot evaluation: June 2026

**Q3 2026 - Decision Point:**
- Pilot results presentation to DG: July 2026
- Go/No-go decision: August 2026
- If GO: Budget approval request for full rollout
- If NO-GO: Back to drawing board (or suffer with SAP)

**Q4 2026 - Q1 2027 - Rollout (if approved):**
- **API integrations go live:** Sept 2026 (SCASEPS webhook, Qlik connector, SSO)
- Department-by-department rollout: Sept 2026 - March 2027
- Full organization live: April 2027 (target)
- SCASEPS integration: Real-time bi-directional sync (no more manual CSV)
- SAP stays: We're NOT decommissioning SAP (political reality)

**Risk Tolerance by Phase:**

**Pilot (High risk tolerance):**
- "Let's try something radical, worst case we learned something"
- Acceptable failure: Yes, if we learn and pivot
- Political exposure: Low (just 1 dept)

**Rollout (Medium risk tolerance):**
- "Need solid proof, but willing to commit if data supports it"
- Acceptable failure: No, but acceptable growing pains
- Political exposure: Medium (affects 5-6 departments)

**Full deployment (Low risk tolerance):**
- "My career is on the line, this MUST work"
- Acceptable failure: Absolutely not
- Political exposure: Extreme (DG presents to Minister, unions watch closely)
- Fallback plan: Must be able to roll back to SAP if disaster

## Decision Makers & Stakeholders

**Formal Decision Committee:**

**1. Director-General (Sophie Claes)** - 35% weight
- Focus: Strategic fit, political risk, ministerial reporting
- Key question: "Can I present this to the Minister as a success story?"
- Veto power: Absolute

**2. Vice DG - Operations (Luc Desmet)** - 25% weight  
- Focus: Operational impact, service continuity, citizen satisfaction
- Key question: "Will this actually improve our SLA compliance and reduce complaints?"
- Concern: "Don't break what's working (even if it's ugly)"

**3. CFO (Isabelle Mertens)** - 20% weight
- Focus: ROI, budget fit, total cost of ownership
- Key question: "Show me the money - how much do we save and when?"
- Concern: €7M IT budget under scrutiny, needs to show results

**4. Me (Marc Dubois, Director Project & IT)** - 15% weight
- Focus: Technical feasibility, architecture, maintainability, integration
- Key question: "Can my team actually manage this without becoming SAP 2.0?"
- Concern: Don't want another vendor lock-in disaster

**5. HR Director (Nathalie Peeters)** - 5% weight
- Focus: Change management, training burden, agent satisfaction
- Key question: "Will agents embrace this or resist?"
- Concern: Union consultation required (by law)

**Informal But Critical Stakeholders:**

**6. Union Representatives (3 unions: ACOD, VSOA, ACV)** - VETO POWER
- Focus: Workload fairness, privacy, job security, stress levels
- Key concern: "Is this AI going to fire people or monitor them 24/7?"
- Reality: If unions say NO → project is dead (legal requirement to consult)
- Historical precedent: Unions killed previous "productivity monitoring" tool in 2022

**7. Department Directors (11 directors, 89 coordinators)** - ADOPTION GATEKEEPERS
- Focus: "Does this make MY job easier or add bureaucracy?"
- Key influencers: Director Legal (35 FTE), Director Tax (63 FTE), Director Client Mgmt (83 FTE)
- Concern: "We've seen 5 IT projects come and go, why is this different?"
- Reality: If coordinators sabotage adoption → project fails regardless of tech quality

**8. Power Users / Early Adopters (15-20 agents)** - CHAMPIONS OR CRITICS
- Who: Younger agents (25-40 years old), tech-savvy, frustrated with SAP
- Role: Will test pilot, give feedback, influence peers
- Make-or-break: If they say "this is worse than SAP" → credibility destroyed

**9. The "Sophie's" (Senior Expert Agents)** - CREDIBILITY VALIDATORS
- Who: 25-30 senior agents (20+ years tenure), respected, skeptical of change
- Role: Opinion leaders, if they approve → others follow
- Concern: "I've used SAP for 15 years, it's terrible but I know it. Why change?"
- Win condition: Show them AI can HELP them (not replace them)

**Decision Process (Realistic):**

**Month 1-2 (Evaluation):**
- Me (Marc): Technical due diligence, vendor demos, reference calls
- CFO: Cost-benefit analysis, budget alignment
- Union: Initial consultation (legally required)

**Month 3 (Pilot Proposal):**
- Present to Decision Committee (DG, Vice DG, CFO, HR, Me)
- Union formal review (4-6 weeks legally mandated consultation)
- Department directors feedback session

**Month 4-6 (Pilot Execution):**
- Weekly steering committee (Vice DG, Me, HR, CFO)
- Bi-weekly user feedback sessions (team leads, agents)
- Monthly DG briefing

**Month 7 (Decision Point):**
- Pilot results presentation to DG
- Union review of pilot data (workload impact, stress levels)
- Department directors vote (formal or informal)
- CFO ROI validation
- DG final decision (with Minister briefing if GO)

**Veto Scenarios:**
- **Union says NO** → Project dead (legal barrier)
- **DG says NO** → Project dead (political barrier)  
- **CFO says NO** → Project paused (budget barrier, but can be overcome with better ROI proof)
- **Vice DG says NO** → Project wounded (can be overruled by DG, but risky)
- **Department directors revolt** → Project fails in execution (even if approved)

## Review Opdracht

Ga door **hele Ticket Masala codebase**, test de applicatie, en schrijf een **eerlijk, gedetailleerd review document** met:

### Structuur:

1. **Executive Summary** (1 page)
   - Overall score /10
   - Top 3 strengths
   - Top 3 dealbreakers
   - Go/No-go recommendation

2. **Functional Review** (5-10 pages)
   - Voor elke must-have requirement: score + concrete examples from code/UI
   - What works well (with evidence)
   - What's missing (be specific)
   - Comparison to SAP/Qlik (where applicable)

3. **Technical Review** (3-5 pages)
   - Architecture quality (can we maintain this in-house?)
   - Scalability (handles 382 users? 85k cases/year?)
   - Security (GDPR compliant?)
   - **Data Import (PILOT - Manual CSV):**
     - CSV upload UI (drag-drop, field mapping wizard)
     - Bulk import performance (can it handle 50k+ rows in one go?)
     - Data validation & error reporting
     - Duplicate detection (don't re-import same cases)
   - **Integration capabilities (POST-PILOT - APIs for Rollout):**
     - REST APIs for SCASEPS bi-directional sync?
     - Qlik Sense API/database connector?
     - SAP document export (BAPI/RFC/REST)?
     - Webhook support for real-time case creation?
     - Authentication (SSO/SAML for existing AD)?
   - **Data Model Flexibility:**
     - Can handle SCASEPS data structure (case IDs, status codes, metadata)?
     - Extensible for custom fields (tax type, region, client risk profile)?

4. **User Experience Review** (2-3 pages)
   - Agent perspective (daily work easier or harder?)
   - Manager perspective (can I actually manage with this?)
   - Admin perspective (configuration hell or reasonable?)

5. **Gap Analysis** (2-3 pages)
   - Must-haves missing: how long to build? (weeks/months)
   - Nice-to-haves missing: can we live without?
   - Deal-sweeteners present: bonus points!

6. **Pilot Feasibility** (2 pages)
   - Can we run 3-month pilot with 63 agents (Tax dept)?
   - **Integration MVP for Pilot (MANUAL, no APIs):**
     - Historical data: One-time Qlik CSV export (3 years client/case history)
     - Daily cases: SAP/SCASEPS CSV export → manual upload (15 min/day by team lead)
     - Client enrichment: Weekly Qlik CSV export for customer profiles
     - Acceptable burden: Yes for 63 users, NO for 382 (must have APIs for rollout)
   - **CSV Import Requirements:**
     - Tool must support: CSV upload UI, field mapping, duplicate detection
     - Data validation: Client ID matching, date formats, status codes
     - Error handling: Invalid rows logged, not rejected entirely
   - What needs to be added FIRST (minimum viable)?
   - Risk assessment (what could go wrong?)
   - Parallel run strategy: SCASEPS stays active, Ticket Masala shadows
   - **Post-pilot deliverable:** API integration specifications for Phase 2 approval

7. **Cost-Benefit Analysis** (1 page)
   - Implementation cost estimate (including SCASEPS/Qlik integration dev)
   - Annual running cost estimate
   - Time savings (hours/week on dispatch, case handling)
   - ROI calculation: savings (consultant reduction + overtime + SLA penalties) vs. new system cost
   - Integration TCO: API maintenance, data sync monitoring, support

8. **Final Recommendation** (1 page)
   - Tier 1: "Buy now, start pilot next month"
   - Tier 2: "Promising, but need X, Y, Z added first (timeline?)"
   - Tier 3: "Good concept, too immature, revisit in 12 months"
   - Tier 4: "Pass, doesn't fit our needs"

**Tone:** Professional maar eerlijk. Ik ben niet hier om beleefd te zijn, ik ben hier om een €200k beslissing te maken die 180 mensen's werk beïnvloedt. Bullshit detector is ON.

**Length:** 15-25 paginas. Gedetailleerd genoeg om aan CFO/directie te presenteren.

**Output format:** Markdown document met:
- Clear headings & subheadings
- Tables voor scores/comparisons
- Code snippets waar relevant (als bewijs)
- Concrete recommendations (niet "consider improving X" maar "add feature X, estimated 6 weeks dev time")

Ga grondig te werk. Ik heb genoeg van sales pitches en vage beloftes. Show me the code, show me what works, be honest about what doesn't.