# Project Atom: Tax Processing Pipeline
**Context:** Industrial automation pipeline (80% auto / 20% human).
**Architecture:** Split into **Core Platform** (Generic Engines) and **Products** (Specific Tax Implementations).
**Flow:** `Douane (Validate) → Codex (Rules) → Dispatch (Route) → Atelier (Review) → Phare (Analyze)`

> **CRITICAL:** Check the active file path. Apply ONLY the rules relevant to the current module defined below.

---

## 1. 🌍 Global Standards
* **Lang:** English (US) for code/comments.
* **Dependency Rule:** `Products` depend on `Core`. **`Core` NEVER depends on `Products`.**
* **Config:** No magic numbers. Use YAML/Env vars.
* **Agents:** Adhere to `@architect`, `@builder`, `@historian`, `@legal-engineer` personas if invoked.

---

## 2. 🏗️ The Core (Generic Platform)
**Trigger:** `src/core/*`
* **Goal:** Reusable engines that work for ANY tax. The Core does not know what "Airbnb" or "Road Tax" is.
* **Agent:** `@architect` / `@builder`

### 2.1) 🛂 Douane (Core Validator)
**Trigger:** `src/core/douane/*`
* **Goal:** Generic validation engine.
* **Stack:** Python 3.11+, Pydantic V2.
* **Rules:**
    1.  **Pattern:** Load schemas dynamically based on Product ID.
    2.  **Layers:** Separate Technical (Encoding) -> Schema (Structure) -> Business (Lookup).
    3.  **Output:** Structured JSON error reports. No data cleaning.

### 2.2) ⚖️ Codex (Core Engine)
**Trigger:** `src/core/codex/*`
* **Goal:** Generic execution engine for fiscal rules.
* **Stack:** Python 3.11+ (Standard Lib only).
* **Rules:**
    1.  **Pure Functions:** Input -> Output. No side effects.
    2.  **Abstraction:** Define Interfaces/Base Classes here. Do not implement specific tax rates here.
    3.  **Testing:** 100% Branch Coverage on engine logic.

### 2.3) 🚦 Dispatch (Routing)
**Trigger:** `src/core/dispatch/*`
* **Goal:** Workload distribution.
* **Stack:** .NET 8 (C#), Python (ML Services).
* **Rules:**
    1.  **Architecture:** Clean Architecture (Domain/App/Infra/API).
    2.  **Logic:** Implement WSJF (Weighted Shortest Job First) prioritization.

### 2.4) 🛠️ Atelier (UI Shell)
**Trigger:** `src/core/atelier/*`
* **Goal:** Generic Human Review Interface.
* **Stack:** Streamlit (Frontend), FastAPI (Backend).
* **Rules:**
    1.  **Layout:** 3-Panel strict (Context | Analysis | Action).
    2.  **Dynamic:** UI components must render based on Product configuration.
    3.  **State:** Optimize `st.session_state` to prevent re-runs.

### 2.5) 🔮 Phare (Calc Engine)
**Trigger:** `src/core/phare/*`
* **Goal:** Monte Carlo Simulation Kernel.
* **Stack:** Python (FastAPI/NumPy).
* **Rules:**
    1.  **Stateless:** Accepts parameters -> Returns Result Set (JSON/Parquet).
    2.  **Vectorized:** Use NumPy/Polars. No Python loops for math.

---

## 3. 🏨 The Products (Verticals)
**Trigger:** `src/products/*`
* **Goal:** Specific implementations of Taxes (e.g., Hotel Tax, Road Tax).
* **Agent:** `@legal-engineer`

### 3.1) ✈️ Airbnb-Pilot (Hotel Tax)
**Trigger:** `src/products/airbnb-pilot/*`
* **Goal:** Domain Model, Rules, and Schemas for Hotel Tax.
* **Rules:**
    1.  **Inheritance:** Implement interfaces defined in `src/core`.
    2.  **Traceability:** Comments in Rules MUST cite legal articles (e.g., `# Art. 44 §2`).
    3.  **Isolation:** Logic here applies ONLY to Hotel Tax.
    4.  **Qlik:** Store product-specific Qlik load scripts here.

---

## 4. 🗄️ Documentation & Strategy
### 4.1) Secretaire (Docs)
**Trigger:** `secretaire/*`
* **Agent:** `@historian`
* **Goal:** Markdown docs, ADRs, Changelogs. Tone: Professional.

### 4.2) StrategyNotes
**Trigger:** `strategynotes/*`
* **Agent:** `@consultant` / `@business-analyst`
* **Goal:** High-level decisions and roadmaps.