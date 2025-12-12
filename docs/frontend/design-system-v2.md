# 🎨 Ticket Masala - Design System v2

A comprehensive design system for building consistent, professional, and on-brand user interfaces.

---

## 🌶️ Brand Colors (Updated)

| Color | Hex | Usage |
|-------|-----|-------|
| Logo Green (Pepper Fill) | `#C8E6C9` | Very light background tints |
| Logo Dark (Tick/Outline) | `#383D39` | Deep charcoal elements |
| Logo Accent Green | `#8BC34A` | Success states, highlights |
| Spicy Orange (Accent) | `#FF8A65` | Notifications, badges, highlights |
| Spicy Orange (Text) | `#BF360C` | Text readability on white backgrounds |

---

## 🌈 Gradients (Updated)

| Token | Hex | Usage |
|-------|-----|-------|
| `--gradient-dispatch` | `linear-gradient(135deg, #4C5AF5 0%, #1E293B 100%)` | Dispatch header gradient |

---

## ✅ Success States

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-success-light` | `#f0fff4` | Selected ticket background |
| `--color-success-border` | `#28a745` | Selected ticket border |

---

## 📏 Typography

### Font Families

| Usage | Font | Fallback Stack |
|-------|------|----------------|
| **Headings** | `Rajdhani` | -apple-system, BlinkMacSystemFont, sans-serif |
| **Body** | `Inter` | -apple-system, BlinkMacSystemFont, Roboto, sans-serif |

---

## Typography (Continued)

| Usage | Font | Fallback Stack |
|-------|------|----------------|
| Headings | Rajdhani | 'Rajdhani', sans-serif |
| Body Text | Inter | 'Inter', sans-serif |

---

## Spacing Guidelines

| Token | Value | Usage |
|-------|-------|-------|
| `--spacing-small` | `4px` | Compact spacing |
| `--spacing-medium` | `8px` | Default spacing |
| `--spacing-large` | `16px` | Generous spacing |

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

## 📚 Implementation

All variables are defined in:

- **CSS**: [`wwwroot/css/design-system.css`](file:///Users/juanbenjumea/coding/projects/ticket-masala/IT-Project2526/wwwroot/css/design-system.css)

Fonts loaded via Google Fonts in `_Layout.cshtml`:

```html
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Rajdhani:wght@500;600;700&display=swap" rel="stylesheet">
```