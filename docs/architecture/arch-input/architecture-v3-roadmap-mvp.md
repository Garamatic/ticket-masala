# Ticket Masala v3.0 - MVP Roadmap

**Version:** 3.0  
**Date:** December 2025  
**Status:** In Progress

---

## Overview

This document covers the **v3.0 MVP** improvements focused on developer experience, observability, and API foundations. These are low-effort, high-impact items that should be completed first.

---

## ⚠️ Critical Architectural Principles

> [!CAUTION]
> The following principles are **non-negotiable** for v3 development.

### 🚫 Principle 1: "Compile, Don't Interpret"

- Use **Expression Trees** to compile YAML rules into delegates at startup
- Store in `ConcurrentDictionary<string, Func<Ticket, bool>>`
- **Status:** ✅ `RuleCompilerService` already implemented

### 💾 Principle 2: SQLite Performance Discipline

- Enable **WAL Mode** for concurrent read/write
- Use **Generated Columns** for JSON field indexing
- Never query directly on non-indexed `json_extract()`

### 🚀 Principle 3: The "Lite Doctrine"

| Area | ❌ Rejected | ✅ Required |
|------|-------------|-------------|
| Queuing | RabbitMQ | `System.Threading.Channels` |
| Caching | Redis | `IMemoryCache` |
| ML | Python Sidecar | ML.NET (In-Process) |

---

## v3.0 Improvements

### 1. Universal Entity Model Terminology ✅

**Status:** Completed

- API supports `/api/v1/tickets` and `/api/v1/workitems` routes
- `CreateWorkItemRequest`/`WorkItemResponse` DTOs added
- Views use domain-configurable labels
- See [ADR-001](./ADR-001-uem-terminology.md)

---

### 2. OpenAPI/Swagger Documentation

**Problem:** No API reference for external integrators.

**Solution:** Add Swashbuckle with XML comments.

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ticket Masala API", 
        Version = "v1"
    });
    c.IncludeXmlComments(Path.Combine(
        AppContext.BaseDirectory, 
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});
```

> [!IMPORTANT]
> XML comments must document which config files govern field values (e.g., `masala_domains.yaml`).

**Effort:** Low (1 sprint) | **Priority:** 🔴 Critical

---

### 3. Structured Logging with Correlation

**Problem:** Hard to trace requests through layers.

**Solution:** Serilog with correlation IDs.

```csharp
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithCorrelationId()
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});
```

> [!IMPORTANT]
> Enrich logs with `TicketId` and `ProjectId` throughout the GERDA pipeline.

**Effort:** Low (1 sprint) | **Priority:** 🔴 Critical

---

### 4. Integration Test Fixtures

**Problem:** Brittle tests with hardcoded data.

**Solution:** Builder pattern for test data.

```csharp
public class TicketBuilder
{
    private readonly Ticket _ticket = new() { /* defaults */ };
    
    public TicketBuilder WithDomain(string d) { _ticket.DomainId = d; return this; }
    public TicketBuilder WithCustomField(string k, object v) { /* ... */ return this; }
    public Ticket Build() => _ticket;
}
```

> [!WARNING]
> `Build()` must sync with SQLite Generated Columns logic.

**Effort:** Low (1 sprint) | **Priority:** 🔴 Critical

---

### 5. ML.NET Model Persistence

**Problem:** Models trained on each startup; slow and loses improvements.

**Solution:** Persist models to disk.

```csharp
public class ModelPersistenceService
{
    public async Task SaveModelAsync(ITransformer model, string name)
    {
        _mlContext.Model.Save(model, null, $"models/{name}.zip");
    }
    
    public ITransformer? LoadModel(string name)
    {
        var path = $"models/{name}.zip";
        return File.Exists(path) ? _mlContext.Model.Load(path, out _) : null;
    }
}
```

> [!IMPORTANT]
> Link model versions to `DomainConfigVersion` for reproducibility.

**Effort:** Low (1 sprint) | **Priority:** 🔴 Critical

---

### 6. Dev Container Configuration

**Problem:** Setup friction for new developers.

**Solution:** VS Code Dev Container.

```json
// .devcontainer/devcontainer.json
{
  "name": "Ticket Masala Dev",
  "image": "mcr.microsoft.com/dotnet/sdk:8.0",
  "postCreateCommand": "dotnet restore && dotnet tool restore"
}
```

**Effort:** Low (0.5 sprint) | **Priority:** 🟢 Low

---

## Implementation Priority

| # | Item | Effort | Status |
|---|------|--------|--------|
| 1 | UEM Terminology | Low | ✅ Done |
| 2 | OpenAPI/Swagger | Low | ✅ Done |
| 3 | Structured Logging | Low | ✅ Done |
| 4 | Test Fixtures | Low | ✅ Done |
| 5 | Model Persistence | Low | ✅ Done |
| 6 | Dev Container | Low | ✅ Done |

---

## Next Steps

After completing v3.0 MVP, proceed to [v3.1+ Roadmap](./v3-roadmap-future.md) for:

- Configuration Hot Reload
- AI Explainability API
- Plugin Architecture
- Prometheus Metrics

---

*See [v3-roadmap-future.md](./v3-roadmap-future.md) for v3.1+ improvements.*
