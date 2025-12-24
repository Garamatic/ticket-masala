# Blueprint: Troubleshooting & Support

A structured guide for diagnosing and resolving common issues in the Ticket Masala environment.

---

## Diagnostic Strategy: "The Three Checks"

When an issue occurs, always verify these three layers first:

1. **The Config Layer:** Is the YAML/JSON valid? Did it compile?
2. **The Data Layer:** Is the SQLite DB reachable? Is WAL mode enabled?
3. **The AI Layer:** Are the ML.NET models loaded? Is the PII proxy blocking calls?

---

## Common Scenarios

### 1. "Configuration Compilation Failed"
**Symptom:** Application fails to start with an `ExpressionTree` error.
- **Cause:** Invalid field name or operator in `masala_domains.yaml`.
- **Fix:** Check the logs for the specific line number. Verify that the referenced `CustomField` exists in the same domain.

### 2. "Gatekeeper Ingestion Delay"
**Symptom:** `202 Accepted` but tickets don't appear for minutes.
- **Cause:** Background channel worker is throttled or the DB is locked.
- **Fix:** check `AiUsageLogs` for errors. Ensure the disk is not at 100% capacity.

### 3. "AI Suggestions are Missing"
**Symptom:** The AI panel shows "Suggestions Unavailable."
- **Cause:** Governance budget cap reached or PII Scrubber triggered a hard block.
- **Fix:** Check the **Compliance Dashboard**. If the budget is exhausted, suggestions will remain disabled until the next month or until the cap is increased.

---

## Essential Commands

```bash
# Verify Log Integrity
grep "ERROR" logs/*.json

# Check SQLite Database
sqlite3 masala.db "PRAGMA integrity_check;"

# Verify YAML Syntax
yamllint config/masala_domains.yaml
```

---

## Obtaining Support

When reporting an issue, please include:
- The **SHA256 Hash** of your current configuration version.
- The **Correlation ID** from the error logs.
- A **Scrubbed Export** of the problematic `WorkItem` payload.

---

## References
- **[System Overview](../SYSTEM_OVERVIEW.md)**
- **[Testing Guide](testing.md)**
