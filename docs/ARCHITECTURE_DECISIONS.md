# Ticket Masala Architecture Decisions

This document records key architectural decisions for the Ticket Masala project.

## ADR-001: DbContext in Domain Layer

**Status**: Accepted

**Context**: Clean Architecture typically recommends placing DbContext in an Infrastructure layer, separate from Domain.

**Decision**: Keep `MasalaDbContext` in `TicketMasala.Domain.Data`

**Rationale**:
1. **Migration History** - Existing EF Core migrations reference the current namespace
2. **Pragmatic Trade-off** - The DbContext correctly uses domain entities
3. **Deployment Simplicity** - Two projects (Domain + Web) are easier to manage than three
4. **Open Source Focus** - Community contributors benefit from simpler structure

**Consequences**:
- Domain project has EF Core dependencies (accepted trade-off)
- Domain entities remain pure POCOs
- Future extraction to Infrastructure possible if project scales significantly

---

## ADR-002: GERDA AI as Internal Engine

**Status**: Accepted

**Context**: AI features could be a separate service or embedded in the main application.

**Decision**: GERDA AI is implemented as `Engine/GERDA/` within the Web project.

**Rationale**:
1. **Low Latency** - In-process calls for ticket processing
2. **Shared Context** - Direct access to DbContext and domain services
3. **Configuration-Driven** - Features toggle via tenant config

**Consequences**:
- AI processing scales with Web instances
- No network overhead for AI operations
- Easier testing and debugging

---

## ADR-003: Multi-Tenant via Configuration Injection

**Status**: Accepted

**Context**: Supporting multiple organizations with different workflows.

**Decision**: Tenant customization via mounted configuration files and plugins.

**Implementation**:
- `/config/masala_config.json` - Instance settings
- `/config/masala_domains.yaml` - Domain workflows
- `TenantPluginLoader` - Runtime plugin discovery

**Consequences**:
- Same Docker image serves all tenants
- Configuration changes without rebuild
- Strong isolation via volume mounts
