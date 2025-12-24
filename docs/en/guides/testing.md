# Blueprint: Testing & Quality Assurance

Ticket Masala maintains high reliability through a tiered testing strategy that covers core logic, AI performance, and system integration.

---

## Testing Philosophy

We believe in **"Practical Verification"**. Tests should focus on the most complex areas of the system: the Rule Compiler, the GERDA Engine, and Data Ingestion.

- **Fastest First:** Unit tests for Expression Tree compilation must run in seconds.
- **AI Correctness:** We test GERDA recommendations against stable datasets to prevent regression.
- **Snapshot Integrity:** Integration tests verify that SAP imports result in accurate, versioned states.

---

## Test Pyramid

### 1. Unit Tests (xUnit)
Focus on isolated logic.
- **Rule Compiler:** Ensuring YAML compiles to the correct LINQ expressions.
- **PII Scrubber:** Verifying regex patterns detect all required sensitive data.
- **Mappers:** Testing the transformation of raw objects into domain entities.

### 2. Integration Tests
Focus on database and service interactions.
- **Repository Layer:** Verifying SQLite FTS5 queries and generated columns.
- **Identity Flow:** Testing RBAC (Role-Based Access Control) across controllers.
- **Gatekeeper Flow:** Testing the async channel from API post to DB save.

---

## Special Testing: AI and Rules

### Compiled Rule Validation
Since business logic is dynamic, we use specialized tests to verify that common configurations produce the expected results.
```csharp
[Fact]
public void Compiler_CompilesWsjfCorrecty()
{
    var rule = "debt_amount / 1000";
    var func = _compiler.Compile(rule);
    var result = func(new Ticket { DebtAmount = 5000 });
    Assert.Equal(5.0, result);
}
```

### GERDA Match Scoring
We maintain a "Golden Dataset" of agent skills and work history to verify that GERDA recommendations remain consistent across code changes.

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test tests/TicketMasala.Tests.Unit

# Run with coverage (requires reportgenerator)
dotnet test /p:CollectCoverage=true
```

---

## Quality Metrics

1. **Rule Safety:** 100% of compiled expression types must be verified.
2. **API Stability:** All public endpoints must return correct status codes (200/202/404/429).
3. **Audit Trail:** Every integration test must verify that an audit entry was created.

---

## References
- **[Development Blueprint](development.md)**
- **[Troubleshooting Guide](troubleshooting.md)**
