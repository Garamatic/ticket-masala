# Configuration & Extensibility Architecture

**Draft Version: 0.1**
**Date:** December 6, 2025

## 1. Vision

To transform Ticket Masala from a static IT ticketing system into a **generic, domain-agnostic work management platform**. The behavior, terminology, and fields of the application should be driven by configuration, not code.

**Key principles:**
- **Zero-Code Customization:** Administrators should be able to define new ticket types, workflows, and fields via `masala_config.json`.
- **Domain Agnostic:** The system should support diverse use cases (e.g., Landscaping Quotes, Government Casework, IT Support) simultaneously.
- **Hot-Swappable:** Configuration changes should be reflected immediately or with a simple restart, without recompilation.

## 2. Architecture Changes

### 2.1. Dynamic Type System (`TicketType`)

**Current State:**
- `TicketType` is likely a hardcoded C# `enum` (e.g., `Incident`, `ServiceRequest`, `Problem`).
- Adding a new type (e.g., `LandscapingQuote`) requires recompilation.

**Target State:**
- `TicketType` becomes a `string` key.
- Valid types are defined in `masala_config.json`.
- Each type defines its own metadata (Label, Icon, Description).

### 2.2. Configuration Schema (`masala_config.json`)

The configuration file will be expanded to include a `TicketTypes` definitions block.

**Proposed Schema:**

```json
{
  "TicketTypes": {
    "ITSupport": {
      "Code": "IT_INCIDENT",
      "Name": "IT Incident",
      "Icon": "fa-laptop",
      "Description": "Report a technical issue with hardware or software."
    },
    "LandscapingQuote": {
      "Code": "LANDSCAPE_QUOTE",
      "Name": "Landscaping Quote",
      "Icon": "fa-tree",
      "Description": "Request a quote for garden work."
    },
    "GovernmentCase": {
      "Code": "GOV_CASE",
      "Name": "Casework",
      "Icon": "fa-landmark",
      "Description": "Citizen inquiry or official casework."
    }
  }
}
```

### 2.3. Dynamic UI Rendering

- **Ticket Creation:** The "Create Ticket" view will dynamically render a dropdown of available `TicketTypes` read from the config.
- **Icons & Labels:** The UI will use the configured `Icon` and `Name` instead of hardcoded strings.

## 3. Implementation Plan

### Phase 1: Dynamic Ticket Types (Current Focus)
1.  **Modify `masala_config.json`**: Add the `TicketTypes` section with initial definitions (IT, Landscaping, etc.).
2.  **Create Configuration Service**: Implement a service to read and provide these types to the application.
3.  **Refactor `Ticket` Model**: Ensure `TicketType` is treated as a string (or flexible enum equivalent) compatible with the config.
4.  **Update UI**: Modify `Ticket/Create.cshtml` to populate the "Type" dropdown dynamics.

### Phase 2: Custom Workflows (Future)
- Define state transitions per ticket type (e.g., `LandscapingQuote` goes `New -> Quoted -> Accepted`, while `ITSupport` goes `New -> InProgress -> Resolved`).

### Phase 3: Custom Fields (Future)
- Allow defining extra data fields per type (e.g., `GardenSize` for landscaping, `OperatingSystem` for IT).

## 4. Progress Tracker

- [ ] [Phase 1] Define `TicketTypes` in `masala_config.json`
- [ ] [Phase 1] Create `ITicketTypeService` to read config
- [ ] [Phase 1] Update `Ticket` model / usage
- [ ] [Phase 1] Integrate into `Ticket/Create` View
