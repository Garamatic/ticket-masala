# Blueprint: System Configuration

This guide provides the technical specifications for configuring Ticket Masala's domains, AI engines, and environmental parameters.

---

## Configuration Philosophy

Ticket Masala follows the **"Configuration-as-Code"** doctrine. By defining business logic in YAML and JSON, we ensure that environments are reproducible, auditable, and easy to migrate.

---

## Configuration Mapping

The system expects configuration files in the directory defined by the `MASALA_CONFIG_PATH` environment variable.

```text
config/
├── masala_domains.yaml    # Business processes, Terminology, Workflows
├── masala_config.json     # Global App Settings, GERDA AI Weights
└── seed_data.json         # (Optional) Initial Database Objects
```

---

## Domain Configuration (`masala_domains.yaml`)

This file governs the "Personality" of your installation.

### 1. Entity Labeling
Customize terminology to fit your vertical.
```yaml
domains:
  GARDENING:
    entity_labels:
      work_item: "Planting Visit"
      work_handler: "Gardener"
```

### 2. Custom Field Definitions
Supported types: `text`, `number`, `select`, `date`, `checkbox`.
```yaml
custom_fields:
  - name: "soil_ph"
    label: "Soil pH"
    type: "number"
    min: 0
    max: 14
```

### 3. Workflow State Machines
Define the valid lifecycle of a work item.
```yaml
workflow:
  states:
    - code: "NEW"
      name: "Unassigned"
    - code: "DONE"
      name: "Archived"
  transitions:
    NEW: ["IN_PROGRESS", "CANCELLED"]
```

---

## GERDA AI Configuration (`masala_config.json`)

This file tunes the "Intelligence" of the system.

### Ranking (WSJF)
```json
"Ranking": {
  "IsEnabled": true,
  "SlaWeight": 100,
  "ComplexityWeight": 5
}
```

### Dispatching
- `MatrixFactorization`: ML-based affinity and skill matching.
- `ZoneBased`: Geographic assignment logic.

---

## Environment Variables

| Variable | Description | Default |
| :--- | :--- | :--- |
| `MASALA_CONFIG_PATH` | Path to config directory | `/app/config` |
| `DB_PASSWORD` | Encrypted DB connection string | (Empty) |
| `GATEKEEPER_API_KEY` | Secret for Ingestion API | (Required) |

---

## Validation & Hot-Reload

> [!TIP]
> **Hot-Reload:** By default, Ticket Masala watches for changes to `masala_domains.yaml`. Save the file, and the engine will automatically re-compile the rules into Expression Trees.

---

## References
- **[Tenants vs Domains](tenants-vs-domains.md)**
- **[Gatekeeper API](gatekeeper-api.md)**
