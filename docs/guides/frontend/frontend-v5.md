# Frontend setup — v5 (aligned to current project)

This document reflects the latest state of the `TicketMasala` codebase, incorporating architectural updates and best practices for the frontend.

## High-level overview

- **App type:** Server-rendered ASP.NET Core MVC app using Razor Views, TagHelpers, and HTMX for interactivity (`src/TicketMasala.Web`).
- **Static assets:** Served from `wwwroot` (JS/CSS/images). The production UI is Razor-based, with minimal reliance on JavaScript frameworks.
- **Frontend responsibilities:** Render pages, provide declarative interactivity (HTMX), and integrate with backend services via Razor controllers returning HTML partials.

## GERDA Dispatch View Updates

- **New Properties:**
  - `Status` added to `TicketDispatchInfo` to support Dispatching and Anticipation states.
- **Design Tokens:**
  - Updated gradient and success state tokens for better accessibility and brand alignment.
- **Dynamic Badges:**
  - `Dispatching` badges now include a pulsing animation.
  - `Anticipation` badges use an outlined style.

## Updated Frontend Tooling

- **Interactivity:** HTMX is used for declarative interactivity (e.g., `hx-post`, `hx-target`).
- **Dependencies:** No Node.js or `package.json`. Libraries like HTMX are served directly from `wwwroot/lib` or CDNs.
- **State Management:** Server-side state with Razor HTML.
- **CSS:** Standard CSS without a build step.

## Accessibility, i18n & localization

- The site uses localized strings (tests reference localized content). Keep localized resources in `Resources/` and ensure the `RequestLocalization` pipeline remains configured in `Program.cs`.
- When editing views, verify localized text via integration tests in `IT-Project2526.Tests`.

---

This document reflects the latest architectural decisions and ensures alignment with the "Masala Lite" monolith approach.