# GERDA: AI Operations Engine (v2.1)

**Project:** Ticket Masala
**Version:** 2.1 (Critique Addressed)
**Last Updated:** December 2025
**Framework Target:** .NET 9 LTS

---

## 1. Executive Summary

GERDA (Groups, Estimates, Ranks, Dispatches, Anticipates) has evolved into a fully **configuration-driven AI pipeline**. This version addresses critical performance and extensibility critiques, specifically removing hardcoded business logic in favor of YAML configuration and compiled rules.

---

## 2. Critical Architectural Decisions

| Concern | v1 "Hardcoded" | v2 "Configurable" (Current) |
|---------|----------------|-----------------------------|
| **Ranking Logic** | C# `if (breach < 1 day) * 10` | **YAML Formula** + Dynamic Multipliers |
| **Duplicate Detection** | Memory-heavy LINQ Query | **Ingestion Content Hash** (SHA256) |
| **Dispatch Weights** | Hardcoded `0.4 * ML + 0.3 * Skill` | **Configurable Weights** in YAML |
| **Expertise Match** | String `Contains()` | **SQLite FTS5** Semantic Search |
| **Model Storage** | Docker Image | **/app/data Volume** (Persists retrains) |
| **Training** | Main Thread | **Background Semaphore** (Low CPU priority) |

---

## 3. The Five Modules (Refactored)

### 3.1 G — Grouping (The Noise Filter)

*Pattern: Ingestion Deduplication*

Instead of scanning history on every ticket (O(N)), we compute a hash at ingestion.

1. **Ingestion:** Compute `SHA256(Description + CustomerId)`.
2. **Storage:** Save to `Ticket.ContentHash` (Indexed).
3. **Check:** `SELECT Id FROM Tickets WHERE ContentHash = @Hash AND Created > @Window`.
4. **Result:** Zero-allocation instant duplicate check.

### 3.2 E — Estimating (The Sizer)

*Pattern: Category Lookup*

Remains simple for now (KISS). Maps keywords/categories to Fibonacci points via YAML config.

### 3.3 R — Ranking (The Prioritizer)

*Pattern: Rule Engine*

**Problem:** Changing "Breach Multiplier" required a redeploy.
**Solution:** Logic is now defined in `masala_domains.yaml` and executed by `RuleCompilerService`.

```yaml
ranking:
  base_formula: "cost_of_delay / job_size"
  multipliers:
    - condition: "days_until_breach <= 0"
      value: 10.0
    - condition: "days_until_breach <= 1"
      value: 5.0
    - condition: "customer_tier == 'VIP'"
      value: 2.0
```

### 3.4 D — Dispatching (The Matchmaker)

*Pattern: Feature-Driven Scoring*

**Problem:** Definition of "Good Match" was hardcoded.
**Solution:** Weights are injected from configuration.

```yaml
dispatching:
  weights:
    ml_score: 0.4
    expertise_match: 0.3
    language_match: 0.2
    geo_match: 0.1
  constraints:
    max_capacity_penalty: 0.5
```

**Implementation Update:**

- **Expertise Matching:** Uses SQLite FTS5 (`MATCH 'Tax OR Fraud'`) instead of costly string contains.
- **Model Training:** Wrapped in `SemaphoreSlim` and run on low-priority threads to prevent web API starvation.

### 3.5 A — Anticipation (The Weather Report)

*Pattern: Time Series SSA*

Uses ML.NET SSA (Singular Spectrum Analysis) to forecast volume.
*Constraint:* Model files stored in `/app/data/models/` to persist across container restarts.

---

## 4. The Feature Extraction Pipeline

To support the above configurable logic, the application uses a dynamic feature extractor.

1. **Config:** User defines `feature_mapping` in YAML.
2. **Extract:** `DynamicFeatureExtractor` converts Ticket -> `float[]` or `Dictionary<string, object>`.
3. **Execute:**
    - **Ranking:** `RuleCompiler` evaluates YAML conditions against extracted dictionary.
    - **Dispatching:** Strategies weight the ML prediction against extracted feature matches.

---

## 5. Deployment Constraints (Single Container)

To maintain the "In-Process" architecture:

1. **File Storage:** All ML models (`.zip`, `.onnx`) MUST reside in `/app/data/`.
2. **Concurrency:** Training jobs must be rate-limited (1 concurrent training job max).
3. **Database:** SQLite in WAL Mode is required for concurrent ML reads + Web writes.

---

## 6. Implementation Checklist

- [ ] **Refactor Grouping:** Add `ContentHash` column and migration.
- [ ] **Refactor Ranking:** Port C# logic to `RuleCompilerService`.
- [ ] **Refactor Dispatching:** Inject `DispatchWeightsOptions` from config.
- [ ] **Infrastructure:** Ensure `/app/data` volume mount for model persistence.
