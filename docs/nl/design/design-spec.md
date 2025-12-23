# 🎨 Design Spec: Ticket Masala – "Eco-Industrial"

**Versie:** 1.0
**Doel:** Transformatie van "Generic SaaS" naar "Premium Utility".
**Kernwoorden:** Precisie, Menselijkheid, Structuur, Betrouwbaarheid.

---

## 1. De Filosofie

We bouwen geen speelgoed voor hackers, maar een **besturingssysteem voor professionals** (ingenieurs, advocaten, overheid). De interface moet voelen als een moderne luchthaven of een architectenbureau: strakke lijnen, duidelijke bewegwijzering, en een fundament van staal en beton, opgewarmd door de unieke merkidentiteit (het Masala-groen).

---

## 2. Kleurenpalet (The Palette)

We stappen af van puur zwart (#000). We gebruiken diepe, zakelijke grijstinten gecombineerd met "High-Vis" groen.

### A. Primary Brand (De Held)

*Gebruik de color-picker op het logo voor de exacte hex-code, richtlijn hieronder.*

* **Masala Chili Green:** `[PICK FROM LOGO]` (Bv. `#4CAF50` of `#2E7D32`)
* *Gebruik:* Primary Buttons, Active States (vinkjes, toggles), Links, 'Succes' meldingen, Neural Network activiteit.

### B. The Base (De Constructie)

* **Deep Charcoal (Canvas):** `#18191F`
* *Gebruik:* Hoofdachtergrond van de applicatie, sidebars.

* **Structural Slate (Lines & Borders):** `#3F414D` of `#8D99AE` (met lage opacity)
* *Gebruik:* Grid lijnen, dividers, randen van inactieve componenten.

### C. The Surface (De Inhoud)

* **Paper / Off-White:** `#F4F6F8` (Licht) OF **Gunmetal Grey** `#212529` (Donker)
* *Keuze:* Voor een fris "Dossier" gevoel, gebruik lichte kaarten op de donkere achtergrond (Hoog Contrast).
* *Gebruik:* Achtergrond van 'Cards', formuliervelden, modals.

### D. Typography Colors

* **Primary Text:** `#FFFFFF` (Op donker) / `#111111` (Op licht)
* **Secondary Text:** `#9CA3AF` (Muted grey)

---

## 3. Typografie (The Voice)

Weg met Rajdhani. We gaan voor technische leesbaarheid met karakter.

### A. Headers (De Architect)

* **Font:** **Space Grotesk** (of *Chivo*)
* **Kenmerk:** Geometrisch, breed, technisch maar vriendelijk.
* **Gewicht:** Bold (700) of Medium (500).
* **Tracking:** Iets nauwer (-0.02em) voor een compacte look.

### B. Body (De Ambtenaar)

* **Font:** **Inter** (of *Manrope*)
* **Kenmerk:** Onzichtbaar, superieure leesbaarheid.
* **Gewicht:** Regular (400) voor tekst, Medium (500) voor labels.

### C. Code & Data (De Machine)

* **Font:** **JetBrains Mono** (of *Space Mono*)
* **Gebruik:** Code snippets, latency metrics, ID-nummers, tags.

---

## 4. UI Componenten & Vormtaal

### A. The "Visible Grid" Layout

De structuur mag gezien worden.

* **Borders:** Gebruik 1px solid borders in *Structural Slate* tussen secties.
* **Geen Shadows:** Vermijd zware drop-shadows. Hou het plat ("Flat Design") of gebruik zeer subtiele borders om diepte te creëren.

### B. Buttons & Interactie

* **Vorm:** Rechthoekig met minimale radius. `border-radius: 4px;` (Geen volledige pill-shapes, dat is te speels).
* **Primary Button:**
* Background: *Masala Chili Green*.
* Text: White (of donker charcoal als het contrast beter is).
* Font: Space Grotesk, Uppercase of Capitalized.

* **Hover States:** Geen kleurverandering, maar een fysieke verplaatsing (bv. `transform: translateY(-2px);`) of een strakkere border.

### C. Cards als "Dossiers"

Feature blokken zijn geen zwevende vlakken, maar dossiers.

* **Styling:**
* Achtergrond: *Gunmetal Grey* (of *Off-White* in light-mode secties).
* Border: 1px solid *Structural Slate*.
* Accent: Een subtiele *top-border* van 2px in *Masala Chili Green* bij actieve of belangrijke kaarten.

### D. Iconografie

* **Stijl:** Line-icons (Stroke), geen ingekleurde vlakken.
* **Kleur:** *Masala Chili Green*.
* **Integratie:** Gebruik elementen uit het logo (de peper, de curve) subtiel als watermerk of bullet-point.

---

## 5. Do's & Don'ts voor de Devs

* **✅ DO:** Laat witruimte (of zwartruimte) ademen. Precisie vereist ruimte.
* **✅ DO:** Lijn alles pixel-perfect uit op het grid. Asymmetrie mag, maar rommel niet.
* **❌ DON'T:** Gebruik geen andere felle kleuren (geen neon blauw of roze). Groen is de enige "pop".
* **❌ DON'T:** Gebruik geen cursieve (italic) teksten, tenzij voor quotes. Hou het rechtop en feitelijk.

---

**Volgende stap:**
Laat je frontend team (of jijzelf) een "Style Tile" maken: één pagina met de nieuwe fonts, kleuren en knoppen naast elkaar. Als die *vibe* goed zit, rol je het uit over de hele site.

*Bonne chance, architect.*
