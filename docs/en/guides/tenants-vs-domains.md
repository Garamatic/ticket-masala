# Concept: Tenants vs. Domains

Ticket Masala utilizes a powerful two-tier configuration model to provide both organization-level isolation and process-level flexibility.

---

## The Innovation: Hierarchical Abstraction

Most systems choose between **Single-Instance** (hard to scale) or **Multi-Tenant** (rigid). Ticket Masala provides both:

- **Tenants:** Physical/Organization-level isolation.
- **Domains:** Logical/Process-level configuration.

---

## Architectural Planes

### 1. The Tenant Plane (Organization)
A **Tenant** represents an organization, company, or distinct entity.
- **Isolation:** Each tenant gets its own isolated database and file system space.
- **Branding:** Custom CSS themes and logos are defined at the tenant level.
- **Security:** User accounts are scoped to their specific tenant.

### 2. The Domain Plane (Process)
A **Domain** represents a specific business process *within* a tenant.
- **Workflow:** Unique state machines (e.g., IT Support vs. Garden Maintenance).
- **Terminology:** Specialized entity labels (e.g., "Ticket" vs. "Quote").
- **AI Strategy:** Different GERDA weights and ranking algorithms.

---

## 📊 Comparison Matrix

| Feature | Tenants | Domains |
| :--- | :--- | :--- |
| **Level** | Organizational | Operational / Process |
| **Data Scope** | Isolated Database | Shared Database (Shared Schema) |
| **Branding** | Professional Identity (CSS/Logo) | UI Context (Labels/Icons) |
| **Best For** | Separate Companies | Separate Departments |
| **Configuration** | `masala_config.json` | `masala_domains.yaml` |

---

## Operational Scenarios

### Scenario A: The SaaS Provider
A service provider hosts Ticket Masala for three different clients:
1. **Tenant A (Healthcare):** High privacy, HIPAA-compliant encryption.
2. **Tenant B (IT MSP):** Five different IT domains (Networking, Cloud, Security).
3. **Tenant C (Retail):** Customer feedback and warranty domains.

### Scenario B: The Municipal Government
A single large **Government Tenant** with multiple departments sharing data:
1. **IT Domain:** Internal helpdesk.
2. **Landscaping Domain:** Public park maintenance.
3. **Tax Domain:** Revenue investigation.
*In this scenario, a manager could potentially see cross-departmental reports because they share the same tenant database.*

---

## Decision Guide

> [!IMPORTANT]
> - **Use a NEW TENANT** if you need strict data isolation, different compliance requirements, or unique branding.
> - **Use a NEW DOMAIN** if you want to share data/users across different workflows but need specialized rules or labels.

---

## References
- **[System Overview](../SYSTEM_OVERVIEW.md)**
- **[Configuration Guide](configuration.md)**
