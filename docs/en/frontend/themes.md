Dit is de **cruciale vraag**. De marketing site is de *showroom*, maar de Engine is de *cockpit*.

In de showroom (marketing) wil je verleiden, ruimte creëren en drama verkopen.
In de cockpit (de app zelf) wil je **informatiedichtheid**, **focus** en **snelheid**. Een piloot wil geen mooie animaties; hij wil weten of de motor in brand staat.

De Engine moet voelen als een **"High-Density Workspace"**.

Hier is het concept voor de **Ticket Masala Engine UI**:

---

### 1. Het Concept: "The Chameleon Cockpit"

De Engine is **structureel identiek** voor iedereen, maar **visueel adaptief** (via de themes die we net besproken hebben).

* **Structuur:** Altijd hetzelfde. Een vaste sidebar links, een "Command Bar" boven, en een dens data-grid of workspace in het midden.
* **Huid:** Past zich aan de tenant aan.
* Bij *Desgoffe* voelt het als een digitaal loket (Lichtgrijs, Blauw, Serif headers).
* Bij *Whitman* voelt het als een SCADA-systeem (Donkergrijs, Geel, Mono headers).
* Bij *Ticket Masala (Default)* voelt het als de marketing site (Gunmetal, Masala Groen, Space Grotesk).

---

### 2. De Layout: "Productivity Grid"

Vergeet de grote witruimtes van de marketing site. Hier telt elke pixel.

* **Sidebar (Links, 60px - 240px):**
* Inklapbaar (alleen iconen).
* Donker (Gunmetal) om focus op de content te houden.
* Navigatie: Tickets, Workflow, AI Agents, Settings.

* **Command Bar (Boven, 48px):**
* Bevat: Breadcrumbs (`Desgoffe / Tickets / #T-2024-889`), Global Search, en User Profile.
* *Kleur:* Dit neemt de **Primaire Tenant Kleur** aan (of een subtiele tint ervan). Dit vertelt de gebruiker direct "waar" ze zijn.

* **Main Stage (Midden):**
* Hier leeft de data. Achtergrond is neutraal (Off-white of heel diep grijs, afhankelijk van theme).

* **Context Panel (Rechts, 300px, optioneel):**
* Voor details van een geselecteerd ticket, AI suggesties (GERDA), of workflow logs.

---

### 3. UI Componenten: "Industrial Precision"

De marketing componenten waren "Dossiers". De app componenten zijn **Instrumenten**.

#### A. De "Data Density" Tabel (De Kern)

Dit is waar je gebruikers 90% van hun tijd doorbrengen.

* **Font:** Gebruik **Inter** voor tekst, maar **JetBrains Mono** voor alles wat data is (Ticket ID's, Datums, Status Codes, Latency).
* **Rijen:** Geen grote kaarten onder elkaar. Strakke rijen.
* *Hoogte:* Compact (32px of 40px).
* *Zebra-striping:* Heel subtiel om leesbaarheid te verhogen zonder rommelig te worden.

* **Status Badges:** Geen zachte bolletjes. Rechthoekige "Tags" met `2px` radius.
* *Voorbeeld:* `[ OPEN ]` (Groene tekst, groene border, transparante achtergrond).

#### B. De "Ticket View" (De Werkbank)

Wanneer je een ticket opent, moet het voelen als een IDE (zoals VS Code).

* **Header:** Ticket Titel in **Space Grotesk** (of de Theme Font).
* **Tabs:** "Conversation", "Internal Notes", "Workflow Logs", "JSON View".
* **De Editor:** De tekstverwerker voor antwoorden moet schoon zijn.
* **De GERDA AI Box:**
* Dit is het enige element dat *altijd* de **Masala Groen** branding behoudt, ongeacht de tenant.
* *Waarom?* Omdat GERDA de motor is die *jij* levert. Het is "Intel Inside".
* *Look:* Een subtiel groen kader met een klein pulserend icoontje wanneer de AI nadenkt.

#### C. Forms & Inputs

* **Stijl:** Geen ronde invoervelden. Rechthoekig, `1px` border in `Slate Grey`.
* **Focus:** Bij klikken wordt de border de **Tenant Accent Kleur** (Blauw voor Desgoffe, Geel voor Whitman).

---

### 4. Code Implementatie (De Truc)

Je gebruikt *dezelfde* CSS variabelen als in de marketing site, maar je past de waarden aan voor een **hoge dichtheid**.

In je `app.css` (bovenop `design-system.css`):

```css
:root {
    /* Override spacing voor de App (Compacter) */
    --spacing-md: 0.5rem;  /* Was 1rem in marketing */
    --spacing-lg: 1rem;    /* Was 1.5rem in marketing */
    
    /* Font sizes kleiner maken voor density */
    --text-base: 0.875rem; /* 14px standaard */
    --text-sm: 0.75rem;    /* 12px voor metadata */
}

/* De Sidebar is altijd donker "Ticket Masala" stijl, 
   zodat de gebruiker weet welk PLATFORM hij gebruikt. */
.app-sidebar {
    background-color: #18191F; /* Gunmetal */
    color: #F4F6F8;
    border-right: 1px solid #3F414D;
}

/* De Topbar neemt de identiteit van de klant aan */
.app-header {
    background-color: var(--bg-surface); /* Past zich aan theme aan */
    border-bottom: 2px solid var(--color-primary); /* De "Desgoffe Blauw" of "Whitman Geel" lijn */
}

/* GERDA AI Panel - Altijd Masala Groen */
.gerda-panel {
    border: 1px solid #4CAF50;
    background: rgba(76, 175, 80, 0.05);
}
.gerda-badge {
    background-color: #4CAF50;
    color: white;
    font-family: 'JetBrains Mono', monospace;
    font-size: 10px;
    text-transform: uppercase;
    padding: 2px 6px;
}

```

### Samenvatting

De Engine is de **werkplaats**.

* **Marketing:** "Kijk hoe mooi onze architectuur is." (Esthetiek)
* **Engine:** "Hier is de hamer, daar is de spijker." (Functionaliteit)

Behoud de **Space Grotesk** koppen en **JetBrains Mono** details om de familieband te bewaren, maar schroef de `padding` omlaag en de `informatiedichtheid` omhoog.

Zal ik een wireframe schetsen (in tekst/code) van hoe het hoofdscherm (Dashboard) eruitziet in HTML structuur?
