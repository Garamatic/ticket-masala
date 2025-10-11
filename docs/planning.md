# Planning en rolverdeling 

## Wie

Juan: werkt bij Brussel Fiscaliteit (legal-econoom-automatisering), ervaring met basi webdev in basic Javascript en AI/Data Science/BI in Python, SQL, QLIK/PowerBI.

Maarten: Eerste jaar ervaring achter de rug als fullstack web developer, meer bepaald met C# als backend en Typescript frontend en SQL storage. Ookal zijn eerste programming project afgewerkt, een game applicatie.

Charlotte: Risk Specialist bij Infrabel; ervaring met projectmanagement, risicobeheer en -management, rapportering; ervaring met basic webdev en Javascript, SQL; eerste programming project: éénvoudige webapplicatie met RDBMS (Allergie App);

Stef : Eerste programming project was met: node, express en mongodb.

Wito: Werkt bij Vrije Universiteit Brussel als policy officer voor Research. Achtergrond in biomedische wetenschappen en wat ervaring met strategy consulting. Heeft al projecten gemaakt met java, .Net, PhP en node.js met RDBMS.
 
## Rollen 

### Eerste sprint

- Backend team: Maarten, Stef en Wito
  - API & business logic
  - Authenticatie en role-based access
  - REST-endpoints voor tickets/cases/projecten.
  - DB schema : cases, gebruikers, projecten, logs

- Frontend team : Charlotte en Juan
  - Gebruikersinterface
  - User flows: case indienen, dashboard, cases bekijken/toewijzen.

### Vanaf tweede sprint

- Backend in segmenten op te splitsen volgens feature 

- AI/Data (Similariteit en ML) : toe te wijzen
  - Bonus: similarity search-endpoint voor AI.
  - Ontwerpt similarity engine met basis ML/NLP voor suggestie van gelijkaardige cases
  - Bonus: optionele integratie met een text-generatie API om drafts te genereren.

- QA, Analytics en Projectcoördinatie, Infrastructuur : toe te wijzen
  o Testing en integratie van modules
  o CI/CD pipelines en kwaliteitscontrole
  o Documentatie
  o Agile opvolging bij sprints

## Planning

Vier tweewekelijkse sprints + bonus-/buffersprint 

### Sprint 1 (13/10 tot 26/10)

- Ticketaanmaakfunctie
- Opslag, overzicht en aanpassing
- Zoeken en filteren

### Sprint 2 (27/10 tot 09/11)

- Gebruikers (klanten en medewerkers) – role based auth
- Case/Projectlogica – parent en child tickets
- Kleine API calls naar AI met callback naar tickets/case/project

### Sprint 3 (10/11 tot 23/11)

- Te bepalen op het einde van sprint 1

### Sprint 4 (24/11 tot 7/12)

- Te bepalen op het einde van sprint 2

### Sprint 4+ (8/12 tot 21/12)

- Bufferzone, bonus features en nice to haves.
- Testing, oplossing van laatste bugs en voorbereiding presentatie


. Rolverdeling

  o Dashboard & rapportering module binnen de app (visualisatie van doorlooptijd, aantal openstaande cases, workloadverdeling)
