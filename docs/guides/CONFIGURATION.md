# Ticket Masala Configuration Guide

Complete guide to configuring Ticket Masala for your use case.

## Table of Contents

1. [Environment Variables](#environment-variables)
2. [Configuration Files](#configuration-files)
3. [Domain Configuration](#domain-configuration)
4. [GERDA AI Settings](#gerda-ai-settings)
5. [Seed Data](#seed-data)
6. [Examples](#examples)

## Environment Variables

### MASALA_CONFIG_PATH

Override the default configuration directory.

**Default behavior:**
- Development: `./config` (relative to project root)
- Docker: `/app/config`

**Usage:**
```bash
# Linux/Mac
export MASALA_CONFIG_PATH=/path/to/your/config

# Windows
set MASALA_CONFIG_PATH=C:\path\to\your\config

# Docker Compose
environment:
  - MASALA_CONFIG_PATH=/app/config-pro
```

### Other Environment Variables

See `.env.example` for all available environment variables:
- `DB_PASSWORD`: Database password
- `EMAIL_IMAP_SERVER`: Email ingestion server
- `EMAIL_USERNAME`: Email ingestion username
- `EMAIL_PASSWORD`: Email ingestion password

## Configuration Files

### File Locations

All configuration files are loaded from the directory specified by `MASALA_CONFIG_PATH`:

```
config/
├── masala_domains.yaml    # Domain definitions (required)
├── masala_config.json     # GERDA AI settings (required)
└── seed_data.json         # Database seed data (optional)
```

### masala_config.json

Main application configuration file.

**Structure:**
```json
{
  "AppInstanceName": "Ticket Masala",
  "AppDescription": "Your description",
  "DefaultSlaThresholdDays": 30,
  "GerdaAI": {
    "IsEnabled": true,
    "SpamDetection": { ... },
    "ComplexityEstimation": { ... },
    "Ranking": { ... },
    "Dispatching": { ... },
    "Anticipation": { ... }
  }
}
```

See `config/masala_config.example.json` for a fully documented example.

### masala_domains.yaml

Defines work domains (IT Support, Landscaping, etc.) with custom workflows and AI strategies.

**Structure:**
```yaml
domains:
  IT:
    display_name: "IT Support"
    description: "Internal IT helpdesk"
    entity_labels:
      work_item: "Ticket"
      work_container: "Project"
      work_handler: "Agent"
    work_item_types: [ ... ]
    custom_fields: [ ... ]
    workflow: { ... }
    ai_strategies: { ... }
    integrations: { ... }

global:
  default_domain: "IT"
  allow_domain_switching: false
  config_reload_enabled: true
```

See `config/masala_domains.yaml` for complete examples.

### seed_data.json

Optional database seed data for development/testing.

**Structure:**
```json
{
  "Admins": [ ... ],
  "Employees": [ ... ],
  "Customers": [ ... ],
  "WorkContainers": [ ... ],
  "UnassignedWorkItems": [ ... ]
}
```

See `config/seed_data.example.json` for a complete example.

## Domain Configuration

### Creating a Custom Domain

1. **Define Entity Labels** (customize terminology):
```yaml
entity_labels:
  work_item: "Service Request"      # Instead of "Ticket"
  work_container: "Garden Zone"     # Instead of "Project"
  work_handler: "Horticulturist"    # Instead of "Agent"
```

2. **Define Work Item Types**:
```yaml
work_item_types:
  - code: QUOTE_REQUEST
    name: "Quote Request"
    icon: "fa-leaf"
    color: "#28a745"
    default_sla_days: 3
  - code: MAINTENANCE
    name: "Scheduled Maintenance"
    icon: "fa-calendar"
    color: "#17a2b8"
    default_sla_days: 7
```

3. **Define Custom Fields**:
```yaml
custom_fields:
  - name: soil_ph
    label: "Soil pH Level"
    type: number
    min: 0
    max: 14
    required: false
  - name: sunlight_exposure
    label: "Sunlight Exposure"
    type: select
    options: ["Full Sun", "Partial Shade", "Full Shade"]
    required: true
```

**Field Types:**
- `text`: Single-line text input
- `textarea`: Multi-line text input
- `number`: Numeric input (with optional min/max)
- `select`: Dropdown (single choice)
- `multi_select`: Dropdown (multiple choices)
- `date`: Date picker
- `checkbox`: Boolean checkbox

4. **Define Workflow States**:
```yaml
workflow:
  states:
    - code: REQUESTED
      name: "Requested"
      color: "#6c757d"
    - code: QUOTED
      name: "Quoted"
      color: "#ffc107"
    - code: COMPLETED
      name: "Completed"
      color: "#28a745"
  
  transitions:
    REQUESTED: [QUOTED, CANCELLED]
    QUOTED: [SCHEDULED, CANCELLED]
    COMPLETED: []
```

5. **Configure AI Strategies**:
```yaml
ai_strategies:
  ranking: 
    strategy_name: "WSJF"
    base_formula: "cost_of_delay / job_size"
    multipliers:
      - conditions:
          - field: "days_until_breach"
            operator: "<="
            value: 0
        value: 10.0
  dispatching: MatrixFactorization
  estimating: CategoryLookup
```

## GERDA AI Settings

### G - Grouping/Spam Detection

Automatically merge duplicate tickets from the same user.

```json
"SpamDetection": {
  "IsEnabled": true,
  "TimeWindowMinutes": 60,
  "MaxTicketsPerUser": 5,
  "Action": "AutoMerge",
  "GroupedTicketPrefix": "[GROUPED] "
}
```

### E - Estimating/Complexity

Estimate effort points based on ticket category.

```json
"ComplexityEstimation": {
  "IsEnabled": true,
  "CategoryComplexityMap": [
    { "Category": "Password Reset", "EffortPoints": 1 },
    { "Category": "Hardware Request", "EffortPoints": 5 },
    { "Category": "System Outage", "EffortPoints": 13 }
  ],
  "DefaultEffortPoints": 5
}
```

### R - Ranking/WSJF

Prioritize tickets using Weighted Shortest Job First.

```json
"Ranking": {
  "IsEnabled": true,
  "SlaWeight": 100,
  "ComplexityWeight": 1,
  "RecalculationFrequencyMinutes": 1440
}
```

**Formula:** `Priority = (SLA_Urgency * SlaWeight) / (Complexity * ComplexityWeight)`

### D - Dispatching/Recommendation

Recommend best agent for each ticket.

```json
"Dispatching": {
  "IsEnabled": true,
  "MinHistoryForAffinityMatch": 3,
  "MaxAssignedTicketsPerAgent": 15,
  "RetrainRecommendationModelFrequencyHours": 24
}
```

**Strategies:**
- `MatrixFactorization`: ML-based recommendations using historical data
- `ZoneBased`: Geographic/regional assignment

### A - Anticipation/Forecasting

Predict future ticket volume and capacity needs.

```json
"Anticipation": {
  "IsEnabled": true,
  "ForecastHorizonDays": 30,
  "InflowHistoryYears": 3,
  "MinHistoryForForecasting": 90,
  "CapacityRefreshFrequencyHours": 12,
  "RiskThresholdPercentage": 20
}
```

## Seed Data

### User Structure

```json
{
  "UserName": "john.doe",
  "Email": "john.doe@example.com",
  "FirstName": "John",
  "LastName": "Doe",
  "Phone": "+1234567890",
  "Code": "JD001"
}
```

### Employee Structure (extends User)

```json
{
  "UserName": "jane.smith",
  "Email": "jane.smith@example.com",
  "FirstName": "Jane",
  "LastName": "Smith",
  "Team": "Support",
  "Level": "Support",
  "Language": "en",
  "Specializations": ["Network", "Security"],
  "MaxCapacityPoints": 40,
  "Region": "EU"
}
```

**Employee Levels:**
- `Support`: Front-line support
- `ProjectManager`: Project management
- `Finance`: Financial operations

### Work Container (Project) Structure

```json
{
  "Name": "Website Redesign",
  "Description": "Complete redesign of company website",
  "Status": "InProgress",
  "CustomerEmail": "client@example.com",
  "ProjectManagerEmail": "pm@example.com",
  "CompletionTargetMonths": 3,
  "WorkItems": [ ... ]
}
```

## Examples

### Example 1: IT Support Domain

See `config/masala_domains.yaml` - IT section.

**Use case:** Internal helpdesk with incidents, service requests, and change management.

### Example 2: Landscaping Services

See `config/masala_domains.yaml` - Gardening section.

**Use case:** Garden maintenance company with quote requests, scheduled maintenance, and pest control.

### Example 3: Custom Government Domain

```yaml
Government:
  display_name: "Citizen Services"
  description: "Municipal service requests"
  
  entity_labels:
    work_item: "Service Request"
    work_container: "Department"
    work_handler: "Case Worker"
  
  work_item_types:
    - code: PERMIT
      name: "Permit Application"
      icon: "fa-file-alt"
      color: "#007bff"
      default_sla_days: 14
    - code: COMPLAINT
      name: "Citizen Complaint"
      icon: "fa-exclamation-circle"
      color: "#dc3545"
      default_sla_days: 7
  
  custom_fields:
    - name: address
      label: "Property Address"
      type: text
      required: true
    - name: ward
      label: "Municipal Ward"
      type: select
      options: ["Ward 1", "Ward 2", "Ward 3", "Ward 4"]
      required: true
```

## Configuration Validation

### Startup Validation

Ticket Masala validates configurations on startup:

```
✓ Configuration base path: /app/config
✓ GERDA config path: /app/config/masala_config.json
✓ GERDA AI Services registered successfully (G+E+R+D+A + Background Jobs)
✓ Domain 'IT Support' configured strategies validated successfully
```

### Common Validation Errors

**"GERDA config not found"**
- Check that `masala_config.json` exists in your config directory
- Verify `MASALA_CONFIG_PATH` is set correctly

**"Domain configuration file not found"**
- Check that `masala_domains.yaml` exists
- Verify file permissions (readable by application)

**"Strategy validation FAILED"**
- Check that referenced AI strategies are implemented
- Valid strategies: `WSJF`, `SeasonalPriority`, `MatrixFactorization`, `ZoneBased`, `CategoryLookup`

## Hot Reload

Configuration changes are automatically detected and reloaded:

```
File watcher enabled for /app/config/masala_domains.yaml
Reloading domain configuration...
Loaded domain configuration with 2 domains: IT, Gardening
```

**Note:** Hot reload can be disabled in production:
```yaml
global:
  config_reload_enabled: false
```

## Troubleshooting

### Configuration not loading

1. Check file paths:
```bash
ls -la $MASALA_CONFIG_PATH
```

2. Check file permissions:
```bash
chmod 644 $MASALA_CONFIG_PATH/*.yaml
chmod 644 $MASALA_CONFIG_PATH/*.json
```

3. Validate YAML syntax:
```bash
# Install yamllint
pip install yamllint

# Validate
yamllint config/masala_domains.yaml
```

4. Validate JSON syntax:
```bash
# Use jq
jq . config/masala_config.json
```

### Changes not taking effect

1. Restart the application (hot reload may be disabled)
2. Check for syntax errors in configuration files
3. Review application logs for validation errors

## Best Practices

1. **Version Control**: Track your configuration files in git
2. **Comments**: Add comments to explain business-specific rules
3. **Validation**: Test configuration changes in staging first
4. **Backups**: Keep backups of working configurations
5. **Documentation**: Document custom domains and strategies
6. **Security**: Never commit secrets to git (use environment variables)
7. **Modularity**: Create separate domains for different business units

## Further Reading

- [Core-Pro Architecture](CORE_PRO_ARCHITECTURE.md) - Two-repository strategy
- [GERDA Documentation](../docs/) - AI strategy details
- [API Documentation](http://localhost:5054/swagger) - REST API reference
