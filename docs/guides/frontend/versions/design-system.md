# 🎨 Ticket Masala - Design System

A comprehensive design system for building consistent, professional, and on-brand user interfaces.

---

## 🌶️ Brand Colors (Logo Extraction)

| Color | Hex | Usage |
|-------|-----|-------|
| Logo Green (Pepper Fill) | `#C8E6C9` | Very light background tints |
| Logo Dark (Tick/Outline) | `#383D39` | Deep charcoal elements |
| Logo Accent Green | `#8BC34A` | Success states, highlights |

---

## 💻 Primary Palette

| Category | Color | Hex | Usage |
|----------|-------|-----|-------|
| **Primary (Action)** | 🔵 Tech Blue | `#4C5AF5` | CTA buttons, links, active indicators |
| **Accent (Spice)** | 🟠 Spicy Orange | `#FF8A65` | Notifications, badges, highlights |
| **Success** | 🟢 Green | `#8BC34A` | Success states, completed status |
| **Warning** | 🟡 Amber | `#FF9800` | Pending states, warnings |
| **Danger** | 🔴 Red | `#F44336` | Errors, destructive actions |
| **Info** | 🔵 Blue | `#2196F3` | Information, in-progress status |

---

## 🌓 Light Mode

| Element | Hex | Notes |
|---------|-----|-------|
| Background | `#FFFFFF` | Pure white, clean professional look |
| Secondary BG | `#F5F7FA` | Cards, panels |
| Primary Text | `#1E293B` | Dark slate for readability |
| Secondary Text | `#64748B` | Subheadings, hints, placeholders |
| Border | `#E2E8F0` | Subtle gray for panels/cards |
| Primary CTA | `#4C5AF5` | Main action color |
| Hover State | `#3D49D0` | Darker blue on hover |

---

## 🌑 Dark Mode

| Element | Hex | Notes |
|---------|-----|-------|
| Background | `#1E293B` | Deep slate (reduced eye strain) |
| Secondary BG | `#0F172A` | Darker panels, modals |
| Primary Text | `#F1F5F9` | Off-white for legibility |
| Secondary Text | `#CBD5E1` | Lighter gray for subtext |
| Border | `#334155` | Subtle borders |
| Primary CTA | `#4C5AF5` | Blue stands out on dark |
| Hover State | `#6577FF` | Luminous lighter blue |

---

## 📝 Typography

### Font Families

| Usage | Font | Fallback Stack |
|-------|------|----------------|
| **Headings** | `Rajdhani` | -apple-system, BlinkMacSystemFont, sans-serif |
| **Body** | `Inter` | -apple-system, BlinkMacSystemFont, Roboto, sans-serif |

### Font Sizes

| Token | Size | Pixels |
|-------|------|--------|
| `--font-size-xs` | 0.75rem | 12px |
| `--font-size-sm` | 0.875rem | 14px |
| `--font-size-base` | 1rem | 16px |
| `--font-size-lg` | 1.125rem | 18px |
| `--font-size-xl` | 1.25rem | 20px |
| `--font-size-2xl` | 1.5rem | 24px |
| `--font-size-3xl` | 1.875rem | 30px |
| `--font-size-4xl` | 2.25rem | 36px |

### Font Weights

| Token | Value | Usage |
|-------|-------|-------|
| `--font-weight-normal` | 400 | Body text |
| `--font-weight-medium` | 500 | Emphasized text |
| `--font-weight-semibold` | 600 | Subheadings |
| `--font-weight-bold` | 700 | Headings |

---

## 📏 Spacing Scale

| Token | Size | Pixels |
|-------|------|--------|
| `--spacing-xs` | 0.25rem | 4px |
| `--spacing-sm` | 0.5rem | 8px |
| `--spacing-md` | 1rem | 16px |
| `--spacing-lg` | 1.5rem | 24px |
| `--spacing-xl` | 2rem | 32px |
| `--spacing-2xl` | 3rem | 48px |
| `--spacing-3xl` | 4rem | 64px |

---

## 🌫️ Shadows

| Token | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` | Subtle lift |
| `--shadow` | `0 1px 3px rgba(0,0,0,0.1)` | Default cards |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.1)` | Elevated cards |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.1)` | Modals, dropdowns |
| `--shadow-xl` | `0 20px 25px rgba(0,0,0,0.1)` | Floating elements |

---

## 🔲 Border Radius

| Token | Value | Usage |
|-------|-------|-------|
| `--border-radius-sm` | 4px | Badges, small buttons |
| `--border-radius` | 8px | Default (buttons, inputs) |
| `--border-radius-lg` | 12px | Cards, panels |
| `--border-radius-xl` | 16px | Modals |
| `--border-radius-full` | 9999px | Pills, avatars |

---

## ⚡ Transitions

| Token | Duration | Usage |
|-------|----------|-------|
| `--transition-fast` | 150ms | Hover states |
| `--transition-base` | 200ms | Default |
| `--transition-slow` | 300ms | Page transitions |

---

## 📐 Z-Index Scale

| Token | Value | Usage |
|-------|-------|-------|
| `--z-dropdown` | 1000 | Dropdown menus |
| `--z-sticky` | 1020 | Sticky headers |
| `--z-fixed` | 1030 | Fixed elements |
| `--z-modal-backdrop` | 1040 | Modal overlays |
| `--z-modal` | 1050 | Modal content |
| `--z-popover` | 1060 | Popovers |
| `--z-tooltip` | 1070 | Tooltips |

---

## 🖼️ Logo Assets

| Asset | Path | Usage |
|-------|------|-------|
| Icon Only | `/images/logo.png` | Favicon, small spaces |
| Full Logo | `/images/full-logo.png` | Headers, branding panels |

---

## 🌈 Gradients

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-gradient-start` | `#667eea` | Dispatch header gradient start |
| `--color-gradient-end` | `#764ba2` | Dispatch header gradient end |

---

## ✅ Success States

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-success-light` | `#f0fff4` | Selected ticket background |
| `--color-success-border` | `#28a745` | Selected ticket border |

---

## 📚 Implementation

All variables are defined in:

- **CSS**: [`wwwroot/css/design-system.css`](file:///Users/juanbenjumea/coding/projects/ticket-masala/IT-Project2526/wwwroot/css/design-system.css)

Fonts loaded via Google Fonts in `_Layout.cshtml`:

```html
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Rajdhani:wght@500;600;700&display=swap" rel="stylesheet">
```
