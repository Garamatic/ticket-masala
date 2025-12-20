ProjectNota: Ticket Masala
Table of Contents
Project Overview
Team Composition
Problem & Solution Analysis
Technical Architecture
Detailed Analysis & Design
Sprint Documentation
1. Projectoverzicht
1.1 Elevator Pitch
Ticket Masala is een AI-gestuurd, flexibel platform voor workflowbeheer. Het werkt op basis van configuratie, waardoor het de vaak nogal rigide, domeinspecifieke IT-ticketing systemen een serieuze boost geeft.

Het systeem lost het probleem van die vastgelegde bedrijfslogica op door YAML-regels om te zetten in snelle C#-code. Bovendien maakt het gebruik van een ingebouwde AI-tool (GERDA) voor slimme triage en prioritering. Zo zijn maximale flexibiliteit, privacy en schaalbaarheid gegarandeerd.

1.2 Wat is het idee?
We trappen de ouderwetse IT-ticketing de deur uit en maken er een superflexibel platform van dat écht elke klus aankan. Of je nu je belastingen moet regelen, een HR-vraag hebt of er een lamp kapot is: het systeem is een kameleon die zich razendsnel aanpast, zonder dat we voor elk wissewasje code moeten schrijven. Kortom: het werkt overal, is supermakkelijk in te stellen en is een eitje om te gebruiken.Wie gebruikt het?
De gebruikers: Burgers en collega's die de services nodig hebben.
De doeners: Supportteams en dossierbeheerders die de processen vlot trekken.
De chefs: Teamleiders die het overzicht houden en de grote lijnen uitzetten.
De tech-helden: Systeembeheerders die ervoor zorgen dat de boel blijft draaien.
Wat kan het? (De Vette Features)
Mix & Match Instellingen: Een slimme motor die met simpele YAML-regels workflows opzet voor compleet verschillende afdelingen. Zie het als Lego: dezelfde blokjes, maar je bouwt er elke keer iets nieuws mee. (Multi-domain configuration)
Slimme Documenten, Direct Actie (DI): Een razendsnelle en vrije manier om direct de essentiële info uit documenten te halen. We combineren OCR (lezen), NLP (taalbegrip) en RAG. Denk aan: even snel de data 'oppeppen'.
Kennis Zo Snel als een Tweet: Een simpele, zelfregulerende kennisbank. Zo is kennis delen en vinden super eenvoudig en snel. Het voelt aan als een vlotte social media-feed (à la Twitter), in plaats van die zware, handmatige documentatie. Echt 'agile' kennisbeheer!

1.3 Multimedia & Validation - script
Ticket Masala: Slimmer, Sneller, Veiliger Ticketing
Wie zijn wij?
Wij zijn Juan, Maarten, Charlotte en Wito, de ontwikkelaars achter Ticket Masala.
Wat is Ticket Masala?
Ticket Masala is een geavanceerd ticketing-platform. Het vervangt rigide, hardgecodeerde processen door flexibele, configureerbare regels. Deze regels worden automatisch en snel omgezet in efficiënte C#-code.
De Rol van AI
Ons ingebouwde, privacy-vriendelijke AI-systeem helpt tickets automatisch te begrijpen, te sorteren en te prioriteren. Dit gebeurt zonder gevoelige gegevens met externe partijen te delen. De AI kan lokaal of met een configureerbare derde partij werken, wat de privacy waarborgt.
De Meerwaarde
Ticket Masala biedt duidelijke meerwaarde op drie vlakken:
Flexibiliteit: Pas nieuwe processen binnen minuten aan, zonder nood aan dure consultants.
Privacy: De AI is diep geïntegreerd en flexibel te configureren voor lokale of externe verwerking van gegevens.
Efficiëntie: Verminder manueel werk en fouten in de ticketverwerking.
Definition of Done
 Een feature is voor ons “klaar” wanneer:
de code proper en getest is
alles werkt in een Docker-omgeving
de API en configuratie duidelijk gedocumenteerd zijn
en de gebruiker duidelijke feedback krijgt in de interface
Geleerde Lessen in dit Project
Tijdens de ontwikkeling hebben we ons gefocust op de volgende leerdoelen:
Het werken met moderne backend-architecturen en flexibele databanken.
De ontwikkeling van een lichte, server-gedreven frontend.
De praktische integratie van AI in een softwareplatform.
Effectieve samenwerking als een multidisciplinair team binnen agile sprints.
Conclusie: Ticket Masala maakt uw ticketing-processen sneller, slimmer en veiliger.

2. Teamleden
Ons team beschikt over een unieke combinatie van diepgaande technische expertise en gespecialiseerde domeinkennis (juridisch, beleid, risicobeheer). Deze multidisciplinaire aanpak is cruciaal voor Ticket Masala: om een systeem te creëren dat zowel aan fiscale als HR-vereisten voldoet, is het essentieel om de taal van beide werelden te beheersen.

Wij functioneren als een hybride team, meer dan alleen een ontwikkelgroep. Maarten en Wito vormen de technische kern en zorgen voor de implementatie ('Het Hoe'), terwijl Juan en Charlotte de focus leggen op de eindgebruiker, de productwaarde en de strategie ('Het Wat' en 'Waarom'). Dankzij deze structuur garanderen we dat we een oplossing bouwen die niet alleen technisch uitmuntend is, maar ook daadwerkelijk relevant en bruikbaar voor de gebruiker.
Naam
Achtergrond & Expertise
Rol binnen Project
Juan
Jurist-Econoom (Brussel Fiscaliteit). Skills: Houdt van een hybride rol (legal/automatisering), duikt graag in AI/Data Science (Python), BI (QLIK/PowerBI), SQL.
Domain & Data Architect. Vertaalt die complexe regels naar easy-peasy logica. Zorgt dat de YAML-configs echt ergens op slaan (oftewel: business sense maken).
Maarten
Fullstack Developer. Skills: C# (.NET), TypeScript, SQL. Heeft al een game-app op zijn naam staan.
Lead Developer & Architect. De technische motor van de ploeg. Houdt zich bezig met de C# backend, zorgt voor een razendsnelle compiler en bewaakt de codestructuur.
Charlotte
Risk Specialist (Infrabel). Skills: Strak Projectmanagement, Risicobeheer, Basic Webdev (JS/SQL). Bouwde eerder een coole Allergie App.
Project Manager & QA. Houdt de scope scherp, checkt de risico's (denk aan Shadow AI) en zorgt dat ons product voldoet aan alle enterprise-eisen (vooral qua rapportage).
Wito
Policy Officer Research (VUB). Skills: Biomedische wetenschappen, Strategy Consulting. Speelt met Java, .NET, PHP, Node.js.
Strategist & Integration Engineer. Kijkt naar het big picture en hoe we dit in andere sectoren kunnen inzetten. Helpt met de backend-integratie.


3. Probleemanalyse en oplossing
3.1 Probleemdefinitie
Problemen met Traditionele Systemen
Traditionele systemen missen de nodige flexibiliteit. Ze kunnen zich niet snel aanpassen aan veranderende behoeften zonder ingrijpende hercodering.
Rigide Datamodellen: Door entiteiten 'hard' in de code vast te leggen, wordt elke simpele uitbreiding (zoals het toevoegen van 'bodem-pH' voor een tuinbouwproject) een omslachtig herprogrammeerproces.
Onderhoudsnachtmerrie: De bedrijfslogica zit vaak diep verborgen in complexe C#-klassen, wat resulteert in moeilijk te onderhouden en ingewikkelde 'spaghetti-code'.
Risico op 'Shadow AI': Het ontbreken van veilige, ingebouwde AI-oplossingen drijft gebruikers naar publieke Large Language Models (LLM's) zoals ChatGPT of Gemini voor gevoelige informatie, wat leidt tot ernstige privacy- en GDPR-risico's. Onze oplossing is de integratie van gecontroleerde calls naar OpenAI, waarbij we prompts kunnen aanpassen en gevoelige data kunnen filteren.
Prestatie-uitdaging:
Het ophalen van niet-geïndexeerde, op maat gemaakte velden uit JSON-bestanden veroorzaakt momenteel aanzienlijke vertragingen. De snelheid van deze operaties moet drastisch verbeterd worden.
3.2 Solution Overview (Het Vierlagenmodel)
Ticket Masala structureert het traject van "Gebruiker" naar "Oplossing" via vier onderling verbonden architecturale lagen.
De Architectuur van Ticket Masala:
De Poort (Ticketing): Intake & Routing
Gebruikers creëren een project of ticket in een dynamische omgeving.
Cruciale stakeholders worden automatisch getagd, waarna het systeem direct overgaat tot automatische toewijzing.
Dit vormt het initiële startpunt van de volledige workflow.
De Werkbank (Case Management): Verwerking & Context
Dit is de cockpit voor de uitvoerende medewerker.
Elk ticket functioneert als een centraal dossier, compleet met de volledige geschiedenis, de real-time status, de verantwoordelijke behandelaar en alle gekoppelde documentatie.
Resultaat: maximale overzichtelijkheid; geen nood aan meerdere tabbladen.
De Controletoren (Project Management): Overzicht & Sturing
Het strategische meta-niveau voor leidinggevenden.
Individuele cases worden hier gebundeld tot gestructureerde projecten.
De focus ligt op het bewaken van de voortgang, het realiseren van Mijlpalen (Milestones) en efficiënt deadline-management
AI-Helper: GERDA (Het Brein): De AI-engine is niet zomaar een chatbot, maar een pijplijn die elke stap in het proces optimaliseert. De naam GERDA staat voor de vijf kernfuncties:
G — Grouping (Groeperen):
Identificeert en clustert gerelateerde tickets (bijv. 50 meldingen over dezelfde server-storing of een reeks gerelateerde belastingdossiers) om dubbel werk te voorkomen.
E — Estimating (Inschatten):
Voorspelt op basis van historische data de benodigde tijd en inspanning (Story Points/Uren) om een ticket op te lossen.
R — Ranking (Rangschikken):
Bepaalt de prioriteit en zoekt de beste 'menselijke' match. Wie heeft de juiste skills en bandbreedte voor dit specifieke probleem?
D — Dispatching (Toewijzen):
De daadwerkelijke actie: het ticket wordt automatisch in de werkvoorraad van de juiste medewerker of team geplaatst.
A — Analyzing (Anticiperen):
Een proactieve waakhond die continu monitort. Dreigt een SLA (Service Level Agreement) verbroken te worden? Is er een escalatie nodig? GERDA waarschuwt voordat het te laat is.
4. Technische architectuur 
4.1 Onze Tech Stack
Ticket Masala? De basis is supersnel, superstabiel en vooral lekker simpel.
De technische smaakmakers:
Onder de motorkap (Backend): We draaien op de nieuwste .NET 10 (ASP.NET Core MVC). Kan niet sneller.
De voorkant (Frontend): Gewoon Razor Views (.cshtml) met een scheutje HTMX.
Ons devies: Server-Side Rendering (SSR) all the way. Pure HTML over de lijn, no heavy JSON.
De Databank: We gebruiken EF Core met SQLite in WAL Mode.
Het slimme trucje: De structuur van ouderwetse SQL, maar de flexibiliteit van NoSQL. Specifieke data parkeren we in CustomFieldsJson, en dankzij Generated Columns (met indexing) vlieg je door die JSON heen.
De Huisvesting (Infrastructuur): Alles is verpakt in Docker containers en gehost op Fly.io. Betrouwbaar en schaalbaar.
AI & Slimme Logica:
ML.NET (In-Process): De AI draait direct binnen de app.
We integreren ook OpenAI voor de handige dingen, zoals tickets samenvatten en projectroadmaps opstellen.
4.2 De Bouwstenen
De Modulaire Monoliet: Lekker Simpel Houden (KISS)
Waarom moeilijk doen met een ingewikkeld web aan microservices? Wij houden het simpel. Eén krachtige applicatie ("In-Process") met een strakke, logische scheiding tussen de onderdelen.
Het voordeel: Veel makkelijker te testen, te installeren (deployen) en je bespaart flink op hostingkosten.
Compileer, Niet Interpreteer: Full Throttle Snelheid
Flexibele systemen worden vaak traag omdat ze regels telkens opnieuw moeten 'doorlezen'. Zonde van de tijd!
Onze aanpak: Die YAML-regels? Die vertalen we bij het opstarten direct naar keiharde C#-code (Func<Ticket, bool>). Geen runtime-overhead. Ons systeem is net zo snel als software die je met de hand codeert.
De 'Lite' Aanpak: Lichtgewicht kampioen
We mijden zware, logge afhankelijkheden. Dat is nergens voor nodig.
Geen RabbitMQ, we gebruiken System.Threading.Channels voor de interne wachtrijen.
Geen Redis, we gebruiken IMemoryCache voor snelle opslag.
Zo blijft de app compact, en de hostingkosten ook. Win-win!

4.3 Haalbaarheid & Risico's
Haalbaarheid
De basis van de architectuur is rock-solid: we gebruiken de bekende patronen van SOA-light (Service Oriented Architecture) en EAV (Entity-Attribute-Value), maar dan in een modern jasje met JSON-extensies. Dit zijn bewezen, stabiele technieken.
Risico's & Hoe We Ze Aanpakken
Risico
Onze Oplossing (Mitigatie)
Trage Regel-Engine Als complexe regels het systeem vertragen.
Compilatie. We zetten de YAML-regels direct om in 'native' Expression Trees. Daardoor voert de processor de logica op maximale snelheid uit, zonder de vertragende stap van interpretatie.
Invoer Overbelasting Wanneer er te veel tickets tegelijk binnenkomen.
Ontkoppeling. We trekken de 'Poortwachter' (Gatekeeper API) los van de 'Verwerker' (Worker Service). De wachtrij vangt de pieken op, zodat de server niet omvalt.
Shadow AI & Data-lekken Gevoelige (persoons)data lekt naar externe partijen.
Tijdelijke Pipeline. Documenten gaan door een 'digitale wasstraat' (OCR/NLP/RAG) waar we eerst alle persoonsgegevens (PII Scrubbing) verwijderen. Pas daarna mag de data naar een externe API.


5. Detailed Analysis & Design
5.1 Design Thinking & Proces
We gebruikten het Double Diamond-model: Divergeren (probleem) en Convergeren (technische oplossing).
Empathize & Define (Probleem):
Pijn: Gebruikers zijn 'gegijzeld' door software; simpele aanpassingen (bv. veld 'Thuiswerkdagen') kosten maanden en consultants.
Definitie: Het probleem is de hard-coded koppeling tussen data en logica.
Ideation (Oplossing):
Alternatieven afgewezen: Optie A (Microservices) was te complex; Optie B (Low-Code) was te duur/traag (vendor lock-in).
Winnaar (Ticket Masala): Een Modular Monolith met een Compiled Rule Engine ("Code-Optional"). Dit combineert de performance van C# met de flexibiliteit van YAML voor business logica.
Ticket Masala is het lichtgewicht, privacy-vriendelijke alternatief voor logge enterprise-oplossingen.
5.2 Market Analysis (SWOT & Competitive Landscape)
Ticket Masala is een gespecialiseerde Decision Support Engine, het privacy-vriendelijke, lichtgewicht alternatief voor logge enterprise-pakketten. We winnen op snelheid, privacy en eigenaarschap, niet op meer features.

SWOT Analyse:

Krachten (Intern)
Zwakten (Intern)
O(1) Performance (gecompileerde YAML)
Complexiteit van Versioning
Privacy-Moat (GDPR-proof AI met PII-scrubbing)
Vereist diepe .NET-kennis (Expression Tree Compiler)
MasalaRank (zelforganiserende Kennisbank)
Geen externe Vendor Support
TCO Voordeel (Geen licentiekosten vs. Jira €15k/jaar)




Kansen (Extern)
Bedreigingen (Extern)
Vraag naar Veilige AI (Shadow AI Paniek)
"Buy vs. Build" Vooroordeel
Digitalisering MKB & Overheid (Digital Sovereignty)
Gevestigde Orde (ServiceNow, Salesforce)
Noodzaak tot snelle procesaanpassing (Agile Wetgeving)
Risico op Feature Creep

Concurrentie Voordelen:
vs. ServiceNow/Salesforce (De Tankers): Ticket Masala is de Speedboot. Wijzigingen kosten minuten, geen maanden.
vs. Jira/Trello (De IT-Tools): Domein-Agnostisch. Spreekt de taal van de gebruiker (HR, Juristen), niet alleen IT-taal.
vs. Mendix/PowerApps (De Gouden Kooi): Brain-in-a-Box. Eenmalige setup, onbeperkt gebruik, geen vendor lock-in of onvoorspelbare licentiekosten. Volledige datacontrole (SQLite).

5.3 User Personas & Entities (Het Universele Model)
Om domein-agnostisch te zijn, gebruikt het systeem een Universal Entity Model (UEM). In de interface worden deze termen "vertaald" naar het jargon van de gebruiker.
Work Item (Het Ticket / Het Dossier)
Definitie: De kleinste eenheid van werk.
Context: In IT is dit een "Bug Report", bij de fiscus een "Bezwaarschrift", in de tuinbouw een "Snoeiopdracht".
Work Container (Het Project / De Portfolio)
Definitie: Een verzameling gerelateerde items.
Context: Bundelt tickets rondom een thema (bijv. "Jaarrekening 2025" of "Migratie Serverpark").
Work Handler (De Behandelaar)
Definitie: De verantwoordelijke agent.
Context: De IT-specialist, de belastingcontroleur of de hovenier.
Customer/Citizen (De Aanvrager)
Definitie: De entiteit die de service initieert.
Context: De burger die een put in de weg meldt, of de medewerker die een nieuwe laptop vraagt.
Hier is een strakke, Nederlandstalige vertaling. Ik heb de zinnen ingekort tot de essentie (Lean & Mean), zodat ze direct scanbaar zijn voor de evaluatoren, zonder de technische nuance te verliezen.

5.4 User Stories
We hebben de user stories ingedeeld volgens de vier lagen van onze architectuur.
Laag 1: Ticketing (De Poort)
Focus: Intake & Toegankelijkheid
US-1.1: Als Burger wil ik een dynamisch formulier dat zich aanpast aan mijn specifieke vraag, zodat ik geen overbodige velden hoef in te vullen.
US-1.2: Als Burger wil ik direct een ontvangstbevestiging met Tracking ID, zodat ik weet dat mijn aanvraag goed is aangekomen.
US-1.3: Als Anonieme Gebruiker wil ik de status bekijken via een publieke link, zodat ik geen account hoef aan te maken om de voortgang te checken.
Laag 2: Case Management (De Werkbank)
Focus: Verwerking & Context
US-2.1: Als Behandelaar wil ik een gecombineerde tijdlijn (mails & notities) zien, zodat ik in één oogopslag de volledige context heb zonder te wisselen van tabblad.
US-2.2: Als Behandelaar wil ik de status wijzigen (bijv. naar 'Wacht op Info'), zodat het ticket automatisch naar de volgende stap in de workflow gaat.
US-2.3: Als Behandelaar wil ik documenten uploaden bij een ticket, zodat alle bewijsstukken centraal bewaard blijven.
Laag 3: Project Management (De Controletoren)
Focus: Overzicht & Meta-Data
US-3.1: Als Teamleider wil ik losse tickets bundelen in een Project, zodat ik de voortgang van grote initiatieven (zoals "Kantoorverhuizing") als geheel kan volgen.
US-3.2: Als Teamleider wil ik een SLA-dashboard zien met tickets die dreigen te verlopen, zodat ik kan ingrijpen voordat deadlines worden gemist.
US-3.3: Als Admin wil ik nieuwe velden definiëren via YAML, zodat ik nieuwe domeinen kan ondersteunen zonder dat een developer de C#-code moet hercompileren.
Laag 4: AI-Helper / GERDA (De Sous-Chef)
Focus: Intelligentie & Versnelling
US-4.1: Als Behandelaar wil ik een AI-conceptantwoord (draft) op basis van historische oplossingen, zodat ik sneller en consistenter kan reageren.
US-4.2: Als Systeem wil ik inkomende tickets automatisch routeren op inhoud (NLP), zodat de tijd voor handmatige triage drastisch vermindert.
US-4.3: Als Risk Officer wil ik dat privacygevoelige data (PII) automatisch wordt gewist vóór externe verwerking, zodat we GDPR-compliant blijven en 'Shadow AI' risico's mitigeren.

5.5 Artifacts & Diagrams (Vervolg)
Flowcharts
De applicatie kent twee primaire stromen die cruciaal zijn voor het begrip van de architectuur:
The Ingestion Flow (De Intake):
User dient formulier in $\rightarrow$ Gatekeeper API valideert invoer tegen YAML-regels $\rightarrow$ Data wordt opgeslagen als WorkItem $\rightarrow$ Bevestiging naar gebruiker.
The Enrichment Pipeline (De Verrijking):
Background Worker ziet nieuw ticket $\rightarrow$ PII Scrubber verwijdert persoonsgegevens $\rightarrow$ GERDA (AI) analyseert tekst $\rightarrow$ Router wijst ticket toe aan agent $\rightarrow$ Status update naar 'Triage Complete'.
Prototypes / Wireframes


Database Schema (ERD)


5.5 API Documentation

[ nog toevoegen ]

5.6 TICT Analysis (Technology Impact Cycle Tool)
De TICT-analyse laat zien dat Ticket Masala scoort als een ethisch en ecologisch bewust project.
1. Environmental Impact (Groen & Licht)
Score: Positief.
Door de "Lite Doctrine" (geen zware cloud-services, efficiënte .NET binary, SQLite) is de CO2-voetafdruk van onze hosting minimaal. De applicatie kan draaien op een Raspberry Pi of een goedkope VPS, in tegenstelling tot zware Java/Enterprise stacks.
2. Privacy & Data (Veilig & Lokaal)
Score: Zeer Positief.
De "Privacy Proxy" architectuur garandeert dat er geen ongeschoonde data naar Big Tech (OpenAI/Microsoft) vloeit. We respecteren de data-soevereiniteit door alles on-premise op te slaan.
3. Human Impact (Bias & Werk)
Score: Aandachtspunt (Neutraal).
Risico: AI-ranking (GERDA) kan onbedoelde bias vertonen (bijv. bepaalde type klachten structureel lager prioriteren).
Mitigatie: We implementeren een "Human-in-the-loop" principe. GERDA doet slechts een aanbeveling; de menselijke agent neemt de definitieve beslissing. Bovendien is het algoritme transparant (White Box AI) en niet een ondoorzichtig neuraal netwerk.
Conclusie:
Ticket Masala is een voorbeeld van "Responsible Innovation". We gebruiken AI om mensen te ondersteunen, niet om ze te vervangen of te bespioneren.
6. Sprint Documentation
6.1 Sprint 1 & 2: Fundering & Flexibiliteit
Periode: [Datums invoegen]
Focus: Opzetten van de architectuur (Modular Monolith), authenticatie en de basis van het datamodel (UEM).
Belangrijkste opleveringen:
Rolgebaseerde Authenticatie: Beveiliging opgezet voor diverse rollen (Admin, Agent, Manager).
UEM Datamodel: Implementatie van Ticket en Project entiteiten.
Custom Fields Engine (Phase 2): Opzet van CustomFieldsJson opslag en dynamische UI-rendering, zodat tickets velden kunnen hebben die niet hard-coded zijn.
Gebruikersbeheer: CRUD-functionaliteit voor interne gebruikers en klanten.
Git Repo: [Insert URL]
Sprint Video: [Insert Sprint 1-2 Video]
Retrospective:
Keep: De keuze voor SQLite in WAL-modus werkt performant voor lokale ontwikkeling.
Problem: Het dynamisch renderen van Razor views op basis van JSON-data was complexer dan verwacht.
Try: In volgende sprints sneller mockups maken voor de frontend flows.

6.2 Sprint 3: API & Workflow Engine
Periode: [Datums invoegen]
Focus: Connectiviteit en bedrijfslogica. Het systeem moet 'praten' met de buitenwereld en regels afdwingen.
Belangrijkste opleveringen:
REST API: Endpoints opgezet voor externe integraties, inclusief Swagger documentatie.
Deployment: Eerste succesvolle uitrol naar productie op Fly.io.
Workflow Engine (Phase 3): Implementatie van IRuleEngineService om statustransities te valideren op basis van YAML-regels.
Rule Compiler (Phase 5): Optimalisatieslag waarbij regels bij startup gecompileerd worden naar Expression Trees voor performance.
Sprint Video: [Insert Sprint 3 Video]
Retrospective:
Keep: De "Compile, Don't Interpret" strategie zorgt voor merkbare snelheidswinst.
Problem: Deployment naar Fly.io gaf initiële configuratie-problemen met volumes.
Try: DevOps taken eerder in de sprint plannen.

6.3 Sprint 4: Intelligence (GERDA) & Search
Periode: [Datums invoegen]
Focus: Het 'slim' maken van de applicatie. Integratie van AI en zoekfunctionaliteit.
Belangrijkste opleveringen:
Zoeken & Filteren: Implementatie van snelle full-text search en filters op JSON-velden.
ML.NET Integratie: Eerste versie van GERDA (in-process AI) voor classificatie taken.
AI Strategies (Phase 4): Configureerbare strategieën (bijv. WSJF vs. Seasonal) per domein.
Feature Extraction (Phase 6): Automatisch omzetten van ticket-data naar float[] voor het ML-model.
Sprint Video: [Insert Sprint 4 Video]
Retrospective:
Keep: ML.NET integratie in-process werkt soepel en voorkomt externe API-kosten.
Problem: Het trainen van modellen vereist meer data dan we initieel hadden (synthetische data moeten genereren).

6.4 Sprint 5: Dashboarding & User Experience
Periode: [Datums invoegen]
Focus: Inzicht en interactie. De gebruiker moet kunnen samenwerken en sturen op data.
Belangrijkste opleveringen:
Manager Dashboard: Visuele weergave van KPI's en audittrails.
Interactie: Reacties, batch-operaties en notificatiesystemen toegevoegd.
UI Vertalingen: De interface meertalig gemaakt ter voorbereiding op de eindpresentatie.
Create Project from Ticket: Workflow toegevoegd om tickets direct te promoveren tot projecten.
Sprint Video: [Insert Sprint 5 Video]
Retrospective:
Keep: De feedback op het dashboard was positief; visueel inzicht helpt bij de verkoop van het product.

6.5 Sprint 6: De Finale (Klantenportaal & Afwerking)
Periode: 8 dec - 21 dec
Focus: De laatste loodjes voor de deadline. Klantfunctionaliteit en presentatie.
Belangrijkste opleveringen:
Klantenportaal: Data-isolatie (klanten zien alleen eigen tickets) en self-service ticket aanmaak.
GERDA Metrics: Visualisatie van backlog en forecast om de AI-waarde aan te tonen.
Landscaping Demo: Een externe demo-site om de API-integratie te bewijzen.
Documentatie: Complete API docs, deployment instructies en demo-script.
Sprint Video: [Insert Sprint 6 / Final Video]
Marketing Video: [Insert Marketing Video]
Retrospective (Pre-Mortem):
Focus: De focus lag op stabiliteit en het wegwerken van "499 errors" en bugs voor de demo.
Result: Het systeem is productie-gereed (Fly.io) en voldoet aan de MVP eisen.

Reflection & Next Steps
Reflectie op het Proces (Lessons Learned)
Het bouwen van Ticket Masala was een oefening in balans: hoe maak je software die alles kan (belastingen, HR, tuinieren), zonder dat het niets goed doet?
1. De "Genericiteit" Paradox
Het lastigste punt was eigenlijk niet de backend-logica, maar hoe flexibel de frontend moest zijn. We hebben ingezien dat een écht domein-onafhankelijk systeem niet alleen de data moet loskoppelen (dat deden we met CustomFieldsJson), maar ook de gebruikersinterface. Het inbouwen van dynamische Razor-views die zich aanpassen aan de YAML-configuratie was lastiger dan gedacht, maar cruciaal om niet vast te zitten aan "hard-coded" dingen.
2. Architectuur als Strategie
De keuze voor een Modular Monolith (in plaats van Microservices) was echt een schot in de roos. Microservices zouden ons team hebben opgezadeld met complexe deployments en ons hebben vertraagd. De monolith daarentegen gaf ons de vrijheid om supersnel te werken aan features zoals de Rule Compiler en GERDA. De "Compile, Don't Interpret" aanpak was geen overbodige luxe, maar echt cruciaal voor de performance van de rule-engine.
3. De "Shadow AI" Realisatie
Tijdens de analyse van het probleem ontdekten we dat onze grootste concurrent niet Jira is, maar ChatGPT. Mensen gebruiken gewoon onveilige AI, want het werkt. Het besef dat we AI niet moeten blokkeren, maar juist via een veilige 'Privacy Proxy' moeten aanbieden, is de belangrijkste troef van dit project geworden.
Next Steps 
Nu de MVP (v3.0) staat, gaan we de focus verleggen van puur functionaliteit naar hoe goed we kunnen integreren en vooral: vertrouwen opbouwen. Onze roadmap leidt ons naar de volgende stappen:
Uitleg-API (De 'Black Box' openen): GERDA geeft nu wel advies, maar de agent snapt niet waarom. Dat is slecht voor het vertrouwen. We bouwen een API die laat zien hoe het systeem tot z'n keuze kwam: "Agent X is gekozen vanwege 85% Skill Match en 50 punten Affiniteit, ook al heeft hij het al druk." Zonder die transparantie ('Waarom?') blijven teamleiders de AI wantrouwen.
Feedback Loop (Leren van fouten): Het systeem moet slimmer worden door het gebruik. Als een teamleider een suggestie van GERDA negeert en handmatig iemand anders kiest, moet het systeem vragen: “Hoezo?” Die feedback (bv. "Agent is ziek" of "Complexiteit verkeerd ingeschat") gebruiken we om het AI-model regelmatig bij te scholen.
Lokale LLM Samenvatting: Kijken of we een klein lokaal taalmodel (zoals Phi-3 of Llama) kunnen inzetten om lange ticket-omschrijvingen automatisch samen te vatten, zonder dat data ons pand verlaat.
17.3 Samenvatting en Toekomstvisie
Ticket Masala is getransformeerd van een eenvoudig ticketsysteem naar een geavanceerde Decision Support Engine. Door een slimme architectuur – gebruikmakend van SQLite, Compiled Rules en (Lokale) AI – hebben we een oplossing gecreëerd die superieur is aan bestaande enterprise-pakketten op het gebied van snelheid, kostenefficiëntie en beveiliging.
De fundamenten zijn solide: de code is volledig in eigen beheer, en de data blijft lokaal.
We zijn volledig voorbereid voor de demonstratie.






