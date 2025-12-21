# Tenants vs Domains: Understanding the Architecture

**Understanding the two-level configuration system in Ticket Masala**

---

## Tenants: Organization-Level Isolation

**Tenant** = **Organization/Company** (e.g., "Government Services", "Healthcare Clinic", "IT Helpdesk")

### What Tenants Provide

Tenants provide **complete isolation** at the organization level:

| Aspect | Description | Example |
|--------|-------------|---------|
| **Data Isolation** | Separate database per tenant | `government/masala.db` vs `healthcare/masala.db` |
| **Configuration** | Own config files | `tenants/government/config/masala_domains.yaml` |
| **Branding** | Custom CSS/theme | `tenants/government/theme/style.css` |
| **Deployment** | Separate Docker container | Port 8081 for government, 8088 for healthcare |
| **Client App** | Optional custom frontend | `tenants/government/client/index.html` |

### Tenant Structure

```
tenants/
├── government/          # Municipal services tenant
│   ├── config/
│   │   ├── masala_config.json      # GERDA AI settings
│   │   └── masala_domains.yaml     # Domain definitions
│   ├── theme/
│   │   └── style.css               # Branding
│   ├── data/
│   │   └── masala.db               # Isolated database
│   └── client/                     # Optional SPA
│
├── healthcare/          # Medical clinic tenant
│   └── ...
│
└── helpdesk/           # IT helpdesk tenant
    └── ...
```

### When to Use Tenants

**Use tenants when:**
- Different organizations need complete data isolation
- Different branding/UI requirements
- Different deployment schedules
- Compliance/regulatory separation required
- Different teams manage different instances

**Example:** A SaaS provider hosting multiple customers would use tenants.

---

## Domains: Business Process Configuration

**Domain** = **Business Process/Workflow** within a tenant (e.g., "IT Support", "HR", "Finance", "Landscaping")

### What Domains Provide

Domains provide **workflow configuration** within a tenant:

| Aspect | Description | Example |
|--------|-------------|---------|
| **Workflows** | State machines, transitions | IT: New → Triaged → In Progress → Done |
| **Custom Fields** | Domain-specific data | IT: `affected_systems`, `os_version` |
| **AI Strategies** | GERDA configuration | Different ranking/dispatching per domain |
| **SLAs** | Service level agreements | IT Incident: 1 day, Service Request: 5 days |
| **Entity Labels** | Terminology | IT: "Ticket", HR: "Case", Gardening: "Service Visit" |

### Domain Configuration

Defined in `masala_domains.yaml`:

```yaml
domains:
  IT:                                    # Domain ID
    display_name: "IT Support"
    entity_labels:
      work_item: "Ticket"
      work_container: "Project"
      work_handler: "Agent"
    
    work_item_types:
      - code: INCIDENT
        name: "Incident"
        default_sla_days: 1
    
    custom_fields:
      - name: affected_systems
        type: multi_select
        options: ["Email", "VPN", "ERP"]
    
    workflow:
      states: ["New", "Triaged", "In Progress", "Done"]
      transitions:
        - from: "New"
          to: "Triaged"
          roles: ["Admin", "Support"]

  HR:                                    # Another domain in same tenant
    display_name: "Human Resources"
    entity_labels:
      work_item: "Case"
      work_container: "Portfolio"
    # ... different workflow, fields, etc.
```

### When to Use Domains

**Use domains when:**
- Same organization has different business processes
- Different workflows needed (IT vs HR vs Finance)
- Different custom fields per process
- Different AI strategies per process
- Want to share users/data across processes

**Example:** A single company tenant with IT, HR, and Finance departments would use domains.

---

## Relationship: Tenant → Domain → Ticket

```
Tenant (Organization)
  └── Domain (Business Process)
       └── Ticket (Work Item)
```

### Example Scenarios

#### Scenario 1: SaaS Multi-Tenant
```
Tenant: "Acme Corp"
  ├── Domain: "IT Support"
  │   └── Tickets: IT incidents, service requests
  └── Domain: "HR Cases"
      └── Tickets: Employee requests, complaints

Tenant: "Beta Inc"
  ├── Domain: "IT Support"
  └── Domain: "Customer Support"
```

**Each tenant has isolated data, but can have similar domains.**

#### Scenario 2: Single Organization, Multiple Processes
```
Tenant: "Municipal Government"
  ├── Domain: "Citizen Services"
  ├── Domain: "Building Permits"
  ├── Domain: "Tax Inquiries"
  └── Domain: "IT Support"
```

**One tenant, multiple domains sharing the same database.**

---

## Comparison Table

| Aspect | Tenant | Domain |
|--------|--------|--------|
| **Level** | Organization | Business Process |
| **Isolation** | Complete (separate DB) | Logical (same DB, different config) |
| **Deployment** | Separate container | Same container |
| **Configuration** | `masala_config.json` + `masala_domains.yaml` | Defined in `masala_domains.yaml` |
| **Data** | Isolated database | Shared database, filtered by `DomainId` |
| **Branding** | Custom CSS/theme | Uses tenant branding |
| **Users** | Separate user base | Can share users across domains |
| **Use Case** | Multi-organization SaaS | Multi-process within organization |

---

## Current Implementation Status

### Current State (Phase 1)
- **Multi-domain support** (single tenant)
- **Multi-tenant via Docker** (separate containers)
- **No runtime tenant resolution** (requires separate containers)

### Future State (Phase 2)
- **Full multi-tenancy** (single container, runtime tenant resolution)
- **TenantId column** in all entities
- **Tenant filtering** in all queries

---

## Decision Guide

### When to Create a New Tenant

Create a **new tenant** when:
- Different organization/company
- Need complete data isolation
- Different compliance/regulatory requirements
- Different deployment schedule
- Different team managing it

**Example:** Hosting separate instances for "Acme Corp" and "Beta Inc"

### When to Create a New Domain

Create a **new domain** when:
- Same organization, different business process
- Different workflow needed
- Different custom fields
- Different AI strategies
- Want to share users/data

**Example:** Same company needs IT Support, HR Cases, and Finance Requests

---

## Practical Examples

### Example 1: Government Tenant with Multiple Domains

```yaml
# tenants/government/config/masala_domains.yaml
domains:
  CITIZEN_SERVICES:
    display_name: "Citizen Services"
    workflow:
      states: ["Submitted", "Under Review", "Resolved"]
  
  BUILDING_PERMITS:
    display_name: "Building Permits"
    workflow:
      states: ["Application", "Review", "Approved", "Rejected"]
  
  TAX_INQUIRIES:
    display_name: "Tax Inquiries"
    workflow:
      states: ["Received", "Processing", "Answered"]
```

**One tenant, three domains, shared database, shared users.**

### Example 2: Healthcare Tenant with Single Domain

```yaml
# tenants/healthcare/config/masala_domains.yaml
domains:
  APPOINTMENTS:
    display_name: "Appointment Scheduling"
    entity_labels:
      work_item: "Appointment Request"
      work_container: "Patient File"
    workflow:
      states: ["Requested", "Scheduled", "Completed", "Cancelled"]
```

**One tenant, one domain, focused use case.**

### Example 3: Multi-Tenant SaaS

```
Tenant: "Customer A"
  └── Domain: "IT Support"

Tenant: "Customer B"
  └── Domain: "IT Support"
```

**Two tenants, same domain type, completely isolated data.**

---

## Getting Started

### Creating a New Tenant

1. Copy template:
   ```bash
   cp -r tenants/_template tenants/my-tenant
   ```

2. Update configuration:
   - Edit `tenants/my-tenant/config/masala_config.json`
   - Edit `tenants/my-tenant/config/masala_domains.yaml`

3. Add to docker-compose.yml:
   ```yaml
   my-tenant:
     <<: *app-base
     ports:
       - "8085:8080"
     volumes:
       - ./tenants/my-tenant/config:/app/inputs/config:ro
       - ./tenants/my-tenant/data:/app/inputs/data:rw
   ```

4. Start:
   ```bash
   docker compose up my-tenant
   ```

### Creating a New Domain

1. Edit tenant's `masala_domains.yaml`:
   ```yaml
   domains:
     MY_DOMAIN:
       display_name: "My Domain"
       workflow:
         states: ["New", "In Progress", "Done"]
   ```

2. Restart tenant (or use hot-reload if implemented)

3. Create tickets with `DomainId = "MY_DOMAIN"`

---

## Related Documentation

- [Tenants README](../tenants/README.md) - Tenant structure and usage
- [Configuration Guide](CONFIGURATION.md) - Domain configuration details
- [Architecture Overview](../architecture/SUMMARY.md) - Overall architecture

---

**Last Updated:** January 2025
