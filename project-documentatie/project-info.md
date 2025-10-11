# Sneuvelnota – IT Project Werktraject 2025/26

## Scope

Discussies bij meetings van 30/09 en 07/10 structureren. Basis voor afspraken.

- 30/09 hadden we de kickoff voor het vak IT Project. Docent Steve Weemaels suggereerde om samen te werken aan een project. Maarten, Charlotte en Juan zaten vervolgens kort samen om te brainstormen en afspraken te maken.

- Op 07/10 tussen 17u-18u30 spreken we met ons 5 (Maarten, Charlotte, Stef, Wito en Juan) af om af te kloppen of en hoe we met elkaar samenwerken. Deze nota dient als aan te vullen (of af te breken) ruwbouw.

## Wie

Juan: werkt bij Brussel Fiscaliteit (legal-econoom-automatisering), ervaring met basi webdev in basic Javascript en AI/Data Science/BI in Python, SQL, QLIK/PowerBI.

Maarten: Eerste jaar ervaring achter de rug als fullstack web developer, meer bepaald met C# als backend en Typescript frontend en SQL storage. Ookal zijn eerste programming project afgewerkt, een game applicatie.

Charlotte: Risk Specialist bij Infrabel; ervaring met projectmanagement, risicobeheer en -management, rapportering; ervaring met basic webdev en Javascript, SQL; eerste programming project: éénvoudige webapplicatie met RDBMS (Allergie App);

Stef : Eerste programming project was met: node, express en mongodb.

Wito: Werkt bij Vrije Universiteit Brussel als policy officer voor Research. Achtergrond in biomedische wetenschappen en wat ervaring met strategy consulting. Heeft al projecten gemaakt met java, .Net, PhP en node.js met RDBMS.

 

## Wat bouwen we ?

“Case & Project Management Systeem met AI-ondersteuning”

### Vier lagen:

1. Ticketing: Gebruikers maken hun project aan waarbij alle stakeholders aangeduid worden. Elke volgende stap is een ticket dat automatisch toegewezen wordt aan een medewerker.
2. Case management: elk ticket bevat geschiedenis, status, toegewezen medewerker, documentatie
3. Projectmanagement: Projectleiders zien de vooruitgang van hun project en met automatisering van het bereiken van mijlpalen, checks
4. AI-helper: voorstellen van oplossing, eerste draft van antwoorden, rapporten, mogelijkheden

### Connectie van lagen met elkaar :

- Gebruikers (burger/klant/medewerker) =>
- Ticketing is toegangspoort: tickets indienen, basis info aanvullen =>
- Case management organiseert opvolging: case status, toewijzing, documentatie (context)
- Project management zorgt voor organisatie op meta-niveau : bundelen van cases, bijhouden deadlines, checken voor mijlpalen, verantwoordelijkeden expliciteren
- AI Helper maakt het unieker als concept en verrijkt elke stap van het proces: similariteit zoeken, suggesties en drafts…

### Openstaande vragen

1. Kiezen we een bepaalde domein / target klant?
   Bv. bedrijf die het voor sales zou gebruiken, overheid die het voor grote projecten of riskbeheer zou gebruiken…

2. Hoe gefaseerd/agile bouwen:

Aanzet tot voorstel.

- Core (Sprint 1):
  o Gebruikers kunnen aanvraag doen (ticket/case)
  o Medewerker kan bekijken, toewijzen en status aanpassen
  o Case database met zoek- en filteropties
  o Authenticatie met rollen

- Uitbreiding (Sprint 2):
  o AI similariteit algo => suggesties van eerdere casussen en antwoorden
  o Projectlaag om cases/tickets te bundelen, deadlines te zetten en aan medewerkers toe te wijzen met interactie
  o Eenvoudige dashboard

Planning:
Sprint 1 (13/10 tot 26/10)

- Ticketaanmaakfunctie
- Opslag en categorisering
- Categorisering aanpassen
- Admin panel

Sprint 2 (27/10 tot 09/11)

- Gebruikers (klanten en medewerkers) – role based auth
- Case/Projectlogica – parent en child tickets
- Kleine API calls naar AI met callback naar tickets/case/project

Sprint 3 (10/11 tot 23/11)

- Te bepalen op het einde van sprint 1

Sprint 4 (24/11 tot 7/12)

- Te bepalen op het einde van sprint 2

Sprint 4+ (8/12 tot 21/12)

- Bufferzone, bonus features en nice to haves.

Afronding en presentatie van het project (22/12 tot 9/1)

- Testing, oplossing van laatste bugs en presentatie

3. Rolverdeling

- Backend team: Maarten, Stef en Wito
  o Verantwoordelijk voor API & business logic
  o Authenticatie en role-based access
  o REST-endpoints voor tickets/cases/projecten.
  o Bonus: similarity search-endpoint voor AI.
  o DB schema : cases, gebruikers, projecten, logs
  o Migraties, queries, indexen

- Frontend team : Charlotte en Juan
  o Gebruikersinterface
  o User flows: case indienen, dashboard, cases bekijken/toewijzen.

- AI/Data (Similariteit en ML) : Juan
  o Ontwerpt similarity engine met basis ML/NLP voor suggestie van gelijkaardige cases
  o Bonus: optionele integratie met een text-generatie API om drafts te genereren.

- QA, Analytics en Projectcoördinatie, Infrastructuur : toe te wijzen
  o Testing en integratie van modules
  o CI/CD pipelines en kwaliteitscontrole
  o Documentatie
  o Agile opvolging bij sprints
  o Dashboard & rapportering module binnen de app (visualisatie van doorlooptijd, aantal openstaande cases, workloadverdeling)

4. Tech stack?
   Frontend in Blazor (.NET compileren naar Javascript/HTML/CSS)

Backend in .NET
AI-integratie met API (goedkoop of lokaal draaien)
Infra: Azure oplossing
