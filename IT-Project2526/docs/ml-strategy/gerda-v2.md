# GERDA: AI Operations Engine (v2)

**Project:** Ticket Masala
**Version:** 2.2 (Feature Extraction Edition)
**Last Updated:** December 2025

---

## 1. Executive Summary

GERDA (Groups, Estimates, Ranks, Dispatches, Anticipates) has evolved from a fixed set of algorithms into a **flexible, configuration-driven AI pipeline**.

In v2, GERDA decouples the *algorithm* (e.g., Matrix Factorization) from the *data source* (e.g., Ticket fields) using a **Feature Extraction Layer**. This allows each Domain (IT, HR, Gardening) to define its own inputs for the AI models without changing code.

---

## 2. The Semantic Shift

| Concept | v1 Implementation | v2 Implementation |
|---------|-------------------|-------------------|
| **Strategy Selection** | Hardcoded `if/else` | **Strategy Factory** (Configurable) |
| **Model Inputs** | Hardcoded Properties (`Priority`, `Size`) | **Feature Extractor** (YAML Definition) |
| **Dispatching** | Rule-based Fallback | Multi-Strategy (ML -> Rule -> Random) |
| **Ranking** | WSJF Only | Pluggable (WSJF, RiskScore, Seasonal) |

---

## 3. Architecture: The Feature Pipeline

The core innovation in v2 is the **Dynamic Feature Extractor**.

### 3.1 Configuration (`masala_domains.yaml`)

Domains define how to transform raw data into machine-readable features.

```yaml
ai_models:
  dispatching:
    features:
      - name: "zone_code_encoded"
        source_field: "zone"  # From CustomFieldsJson
        transformation: "one_hot"
        params: { target: "Z1" }
      - name: "urgency_norm"
        source_field: "urgency"
        transformation: "min_max"
        params: { min: 0, max: 10 }
```

### 3.2 Feature Extractor Service

The `DynamicFeatureExtractor` reads this config and produces a normalized float vector.

```csharp
// Input: Ticket { CustomFields: { "zone": "Z1", "urgency": 9 } }
// Config: Mapping rules above
// Output: float[] { 1.0, 0.9 }
```

This vector is then passed to the ML Strategy (e.g., ML.NET Prediction Engine).

---

## 4. The Five Modules (Revised)

### 4.1 G — Grouping (Spam & Cluster)

*Status: Configurable Strategies*

- **Strategies:**
  - `TimeWindowClustering`: Groups tickets from same user within X minutes.
  - `TextSimilarity`: (Future) Uses TF-IDF to find duplicate content.

### 4.2 E — Estimating (Sizing)

*Status: Domain-Specific Lookups*

- **Strategies:**
  - `CategoryLookup`: Maps "Password Reset" -> 1 point.
  - `LlmEstimator`: (Experimental) Sends description to LLM for fibonacci guess.

### 4.3 R — Ranking (Prioritization)

*Status: Pluggable Formulas*

- **Strategies:**
  - `WSJF` (IT Default): Cost of Delay / Job Size.
  - `RiskScore` (Tax Default): (Value * RiskFactor) + Deadline.
  - `SeasonalPriority` (Gardening): Boosts maintenance in Spring/Summer.

### 4.4 D — Dispatching (Assignment)

*Status: Feature-Driven ML*

- **The Flow:**
    1. **Extract Features:** `DynamicFeatureExtractor` converts ticket to vector.
    2. **Predict Scores:** `MatrixFactorizationStrategy` predicts Agent affinity.
    3. **Apply Constraints:** Filter by Availability, Role, and Language.
    4. **Fallback:** If ML uncertain, fall back to `RoundRobin` or `LeastLoaded`.

### 4.5 A — Anticipation (Forecasting)

*Status: Time Series*

- **Strategies:**
  - `SsaForecasting`: Singular Spectrum Analysis on historical volume.
  - `MovingAverage`: Simple baseline for low-volume domains.

---

## 5. Developer Guide: Adding a Strategy

To add a new AI capability (e.g., "Sentiment Analysis Ranking"):

1. **Implement Interface:** Create class implementing `IJobRankingStrategy`.
2. **Register:** Add to DI Container with a Key (e.g., `"SentimentRank"`).
3. **Configure:** Update `masala_domains.yaml` to use `ranking: SentimentRank`.
4. **Define Features:** (Optional) Add feature mappings if the strategy needs specific inputs.

---

## 6. Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| **Strategy Factory** | ✅ Complete | Resolves by string key |
| **Feature Extractor** | ✅ Complete | Supports OneHot, MinMax, Bool |
| **Dispatching Integration**| ✅ Complete | Matrices + Fallbacks |
| **Ranking Integration** | ✅ Complete | WSJF fully ported |
| **Estimating Integration**| ✅ Complete | Category lookup |
| **ML Training Pipeline** | 🟡 Partial | Manual trigger only |

---

## 7. Future Directions

- **Auto-ML:** Allow GERDA to self-select the best model for a domain based on accuracy metrics.
- **Model Hosting:** Move high-memory models (LLMs) to a sidecar process if "In-Process" limits are hit.
- **Feedback Loop:** Explicit "Good/Bad Recommendation" buttons for Reinforced Learning.
