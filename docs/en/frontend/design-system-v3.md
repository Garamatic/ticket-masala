# 🎨 Ticket Masala - Design System v3.0

**Codenames:** *Eco-Industrial / Premium Utility*

## 1. Quick Setup (Voor in `_Layout.cshtml`)

Vervang je oude font-link door deze nieuwe combinatie. We introduceren **Space Grotesk** voor technische autoriteit en **JetBrains Mono** voor data-precisie.

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=JetBrains+Mono:wght@400;500&family=Space+Grotesk:wght@500;600;700&display=swap" rel="stylesheet">

```

---

## 2. CSS Variables (Voor in `wwwroot/css/design-system.css`)

Kopieer dit volledige `:root` blok over je oude versie heen.

```css
:root {
    /* --- 🌶️ BRAND IDENTITY (The Hero) --- */
    /* Pas deze hex aan naar de exacte kleurpicker waarde van je logo */
    --color-masala-green: #4CAF50;       /* Primary Action Color */
    --color-masala-hover: #43A047;       /* Darker shade for interaction */
    --color-masala-surface: rgba(76, 175, 80, 0.12); /* Subtle tint for backgrounds */
    
    /* --- 🏭 INDUSTRIAL BASE (The Factory) --- */
    /* We stappen af van blauw-zwart naar neutraal gunmetal/charcoal */
    --bg-canvas: #18191F;         /* Main App Background (Deepest) */
    --bg-surface: #212529;        /* Cards, Sidebars, Panels */
    --bg-surface-elevated: #2C3036; /* Modals, Dropdowns */

    /* --- STRUCTURE (The Grid) --- */
    /* Zichtbare lijnen zijn cruciaal voor de "Technical" look */
    --border-structural: #3F414D; /* Strong borders (Grid lines) */
    --border-subtle: #2A2D35;     /* Subtle dividers */

    /* --- ✍️ TYPOGRAPHY (The Manual) --- */
    --font-heading: 'Space Grotesk', sans-serif; /* Tech-savvy & structured */
    --font-body: 'Inter', sans-serif;            /* Neutral & legible */
    --font-mono: 'JetBrains Mono', monospace;    /* Data, IDs, Metrics */

    /* --- 📝 TEXT COLORS --- */
    --text-primary: #F4F6F8;      /* Off-white (Better contrast than grey) */
    --text-secondary: #9CA3AF;    /* Metallic Grey */
    --text-muted: #6B7280;        /* Disabled / Subtle details */
    --text-inverse: #FFFFFF;      /* Text on Green buttons */

    /* --- 🚦 STATUS INDICATORS --- */
    --status-success: var(--color-masala-green);
    --status-warning: #FFB703;    /* Amber Signal (Industrial Warning) */
    --status-error: #EF4444;      /* Industrial Red */

    /* --- 📐 SHAPE & SPACING --- */
    --radius-sm: 2px;             /* Sharp, precise corners */
    --radius-md: 4px;             /* Standard component radius */
    --radius-lg: 8px;             /* Modal/Large container radius */
    
    --spacing-xs: 0.25rem;        /* 4px */
    --spacing-sm: 0.5rem;         /* 8px */
    --spacing-md: 1rem;           /* 16px */
    --spacing-lg: 1.5rem;         /* 24px */
    --spacing-xl: 2.5rem;         /* 40px */
}

```

---

## 3. Utility Classes (Copy-Paste Ready)

Voeg deze classes toe onder je variabelen. Dit geeft je direct de juiste look zonder dat je elke `div` apart hoeft te stylen.

### Typography

```css
body {
    background-color: var(--bg-canvas);
    color: var(--text-primary);
    font-family: var(--font-body);
    -webkit-font-smoothing: antialiased;
}

h1, h2, h3, h4, h5 {
    font-family: var(--font-heading);
    font-weight: 600;
    color: var(--text-primary);
    letter-spacing: -0.02em; /* Maakt headers compacter/strakker */
    margin-bottom: var(--spacing-md);
}

.text-mono {
    font-family: var(--font-mono);
    font-size: 0.9em;
    color: var(--color-masala-green); /* Data springt eruit */
}

.text-muted { color: var(--text-secondary); }

```

### Buttons (Industrial Style)

Geen ronde knoppen meer. Strak en rechthoekig met een kleine radius.

```css
.btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0.75rem 1.5rem;
    font-family: var(--font-heading); /* Gebruik Space Grotesk in knoppen */
    font-weight: 600;
    border-radius: var(--radius-md);
    transition: all 0.2s ease;
    text-decoration: none;
    cursor: pointer;
    border: 1px solid transparent;
}

/* Primary Action */
.btn-masala {
    background-color: var(--color-masala-green);
    color: var(--text-inverse);
    text-transform: uppercase;
    font-size: 0.875rem;
    letter-spacing: 0.05em;
}

.btn-masala:hover {
    background-color: var(--color-masala-hover);
    transform: translateY(-1px);
}

/* Secondary / Outline */
.btn-outline {
    background-color: transparent;
    border: 1px solid var(--border-structural);
    color: var(--text-primary);
}

.btn-outline:hover {
    border-color: var(--color-masala-green);
    color: var(--color-masala-green);
}

```

### Cards as "Dossiers"

Vervang je oude `.card` styles met dit.

```css
.card-dossier {
    background-color: var(--bg-surface);
    border: 1px solid var(--border-structural);
    border-radius: var(--radius-md);
    padding: var(--spacing-lg);
    position: relative; /* Voor eventuele labels/badges */
    transition: border-color 0.2s ease, transform 0.2s ease;
}

/* Het interactieve effect: rand wordt groen */
.card-dossier:hover {
    border-color: var(--color-masala-green);
    transform: translateY(-2px);
    box-shadow: 0 4px 20px rgba(0,0,0,0.3);
}

/* Een "Header" balkje in de kaart (optioneel) */
.card-dossier-header {
    border-bottom: 1px solid var(--border-structural);
    padding-bottom: var(--spacing-md);
    margin-bottom: var(--spacing-md);
    display: flex;
    justify-content: space-between;
    align-items: center;
}

```

### Grid Layout (The Blueprint)

Gebruik dit voor secties om die "blauwdruk" lijnen te krijgen.

```css
.section-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1px; /* Kleine gap om de border kleur te laten zien */
    background-color: var(--border-structural); /* De lijnen tussen de blokken */
    border: 1px solid var(--border-structural);
}

.section-grid > * {
    background-color: var(--bg-canvas); /* Blokken zelf weer donker maken */
    padding: var(--spacing-xl);
}

```

---

## 4. Migratie Handleiding

Hier is wat je moet zoeken en vervangen in je HTML/Razor files:

| Oude Class/Stijl | **Vervang door Nieuwe Class** |
| --- | --- |
| `font-family: 'Rajdhani'` | (Automatisch geregeld via `h1-h6` styles) of `.font-heading` |
| `text-orange` / `text-warning` | `.text-mono` (als het data is) of `.text-muted` |
| `bg-green-500` (Tailwind style) | `.btn-masala` |
| `card` / `p-4 bg-slate-800` | `.card-dossier` |
| `rounded-full` | `rounded-sm` (of verwijder class, gebruik CSS defaults) |
| Blauwe links/tekst | Verwijder inline styles, laat fallback naar groen/wit werken |

**Actie voor nu:**
Plak de CSS variabelen in je stylesheet. Ziet de site er meteen "strakker" en donkerder uit? Als het groen te fel steekt op de donkere achtergrond, dim het ietsje naar een `opacity: 0.9`.
