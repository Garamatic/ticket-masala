# Rule Configuration YAML Schema

**Purpose:** Define the structure and implementation approach for configuration-driven business rules.

---

## 1. Overview

The Rule Engine allows business logic to be expressed in YAML configuration files rather than code. This enables:

- Non-developers to modify rules
- Domain-specific behavior without redeployment
- Audit trail of rule changes

---

## 2. Implementation Strategy

### 2.1 The `IRuleEngineService` Interface

```csharp
public interface IRuleEngineService
{
    bool CanTransition(Ticket ticket, string targetState, ClaimsPrincipal user);
    IEnumerable<string> GetRequiredFieldsForState(string domainId, string state);
    double CalculatePriority(Ticket ticket);
    ValidationResult ValidateCustomFields(Ticket ticket);
}
```

### 2.2 Field Access Strategy

To evaluate conditions like `case_value > 100000`, the rule engine must access fields from **two sources**:

| Field Type | Location | Access Method |
|------------|----------|---------------|
| **Universal Fields** | `Ticket` entity properties | C# Reflection |
| **Custom Fields** | `CustomFieldsJson` blob | JSON Deserialization |

**Implementation Approach:**

```csharp
// 1. Universal Field Check (Reflection)
var property = ticket.GetType().GetProperty(fieldName);
if (property != null)
{
    var value = property.GetValue(ticket);
    return EvaluateCondition(value, condition.Operator, condition.Value);
}

// 2. Custom Field Check (JSON)
if (!string.IsNullOrEmpty(ticket.CustomFieldsJson))
{
    var customFields = JObject.Parse(ticket.CustomFieldsJson);
    if (customFields.TryGetValue(fieldName, out var token))
    {
        return EvaluateCondition(token.Value<object>(), condition.Operator, condition.Value);
    }
}

// 3. Role Check (ClaimsPrincipal)
if (!string.IsNullOrEmpty(condition.Role))
{
    return user.IsInRole(condition.Role);
}
```

---

## 3. YAML Rule Schema

### 3.1 Transition Rules

Control when state transitions are allowed.

```yaml
workflow:
  transitions:
    FROM_STATE:
      - to: TARGET_STATE
        conditions:
          - field: field_name
            operator: ">" | "<" | "==" | "!=" | "is_not_empty" | "is_empty" | "in" | "contains"
            value: comparison_value  # Optional for is_empty/is_not_empty
          - role: required_role_name  # Role-based condition
        actions:  # Optional: triggered on successful transition
          - type: webhook | email | set_field
            config: { ... }
```

**Example:**

```yaml
transitions:
  UnderReview:
    - to: Escalated
      conditions:
        - field: case_value
          operator: ">"
          value: 100000
        - role: SeniorOfficer
      actions:
        - type: email
          template: "escalation_notice"
          to: "{{assigned_officer.email}}"
    
    - to: PendingDocuments
      conditions:
        - field: documents_complete
          operator: "=="
          value: false
```

### 3.2 Supported Operators

| Operator | Description | Applicable Types |
|----------|-------------|------------------|
| `>` | Greater than | Number, Date |
| `<` | Less than | Number, Date |
| `>=` | Greater than or equal | Number, Date |
| `<=` | Less than or equal | Number, Date |
| `==` | Equals | All |
| `!=` | Not equals | All |
| `is_empty` | Field is null or empty | All |
| `is_not_empty` | Field has value | All |
| `in` | Value in list | String, Number |
| `not_in` | Value not in list | String, Number |
| `contains` | String contains | String |
| `matches` | Regex match | String |

### 3.3 Validation Rules

Field-level validation beyond type constraints.

```yaml
validations:
  - field: tax_code_reference
    rules:
      - type: required
        message: "Tax code reference is mandatory"
      - type: regex
        value: "^IRC-\\d{3,4}$"
        message: "Must be in format IRC-XXX or IRC-XXXX"
    when:  # Conditional validation
      - field: work_item_type
        operator: "=="
        value: "DISPUTE"
```

### 3.4 Automation Rules (Event Triggers)

Actions triggered by system events.

```yaml
automations:
  - name: "Auto-escalate high-value cases"
    trigger: on_create | on_update | on_status_change | scheduled
    conditions:
      - field: case_value
        operator: ">"
        value: 1000000
    actions:
      - type: set_field
        field: GerdaTags
        value: "High-Value-Audit"
      - type: notify
        to: "audit_committee"
        template: "high_value_alert"
```

### 3.5 SLA Rules

Dynamic SLA calculation based on conditions.

```yaml
sla:
  default_days: 7
  overrides:
    - when:
        field: work_item_type
        operator: "=="
        value: "INCIDENT"
      then:
        days: 1
    - when:
        field: priority
        operator: "=="
        value: "Critical"
      then:
        days: 0.5  # 12 hours
```

---

## 4. Complete Example: TaxLaw Domain Rules

```yaml
# masala_domains.yaml (TaxLaw section)
TaxLaw:
  # ... other config ...
  
  rules:
    transition_rules:
      Filed:
        - to: UnderReview
          conditions: []  # No conditions, always allowed
      
      UnderReview:
        - to: PendingDocuments
          conditions:
            - field: documents_complete
              operator: "=="
              value: false
        
        - to: Escalated
          conditions:
            - field: case_value
              operator: ">"
              value: 100000
            - role: SeniorOfficer
        
        - to: Resolved
          conditions:
            - field: case_value
              operator: "<="
              value: 100000
    
    validation_rules:
      - field: tax_code_reference
        rules:
          - type: required
          - type: regex
            value: "^IRC-\\d{3,4}$"
      
      - field: case_value
        rules:
          - type: required
          - type: min
            value: 0
    
    automation_rules:
      - name: "Flag million-dollar audits"
        trigger: on_create
        conditions:
          - field: work_item_type
            operator: "=="
            value: "AUDIT"
          - field: case_value
            operator: ">"
            value: 1000000
        actions:
          - type: set_field
            field: priority
            value: "Critical"
          - type: notify
            to: "audit_committee"

    sla_rules:
      default_days: 30
      overrides:
        - when:
            field: work_item_type
            operator: "=="
            value: "REFUND"
          then:
            days: 14
```

---

## 5. Implementation Considerations

### 5.1 Libraries to Consider

| Library | Purpose |
|---------|---------|
| **Newtonsoft.Json (JObject)** | Parse `CustomFieldsJson` for dynamic access |
| **System.Linq.Dynamic.Core** | Build dynamic LINQ expressions from rules |
| **RulesEngine (Microsoft)** | Full-featured rules engine with JSON/YAML support |

### 5.2 Performance

- **Cache parsed rules** in memory (already done via `IDomainConfigurationService`)
- **Pre-compile expressions** for frequently evaluated conditions
- **Lazy evaluation** - stop on first failing condition

### 5.3 Security

- Sanitize regex patterns to prevent ReDoS attacks
- Validate role names against ASP.NET Identity
- Log all rule evaluations for audit

---

## 6. Phase 2 Implementation Tasks

1. [ ] Create `Condition`, `TransitionRule`, `ValidationRule` model classes
2. [ ] Implement `RuleEngineService` with dual-path field access
3. [ ] Add rule parsing to `DomainConfigurationService`
4. [ ] Integrate `CanTransition()` into `TicketService.UpdateStatus()`
5. [ ] Add UI indicators for valid next states
6. [ ] Write unit tests for rule evaluation
