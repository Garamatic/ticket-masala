# Localhost Demo Configuration

This directory contains generic configuration files for running Ticket Masala on localhost for demo purposes.

## Quick Start

To use the localhost demo configuration:

### Option 1: Rename Files (Recommended for demo)

```bash
# Backup original files
cp seed_data.json seed_data.azure.json.bak
cp masala_domains.yaml masala_domains.azure.yaml.bak

# Use localhost config
cp seed_data.localhost.json seed_data.json
cp masala_domains.localhost.yaml masala_domains.yaml
```

### Option 2: Update appsettings.json

Modify `src/TicketMasala.Web/appsettings.json` to point to the localhost config files:

```json
{
  "Masala": {
    "ConfigPath": "./config",
    "SeedDataFile": "seed_data.localhost.json",
    "DomainsFile": "masala_domains.localhost.yaml"
  }
}
```

## Demo Users

### Admin
- **Username:** admin
- **Email:** admin@localhost.dev
- **Password:** (set during first run)

### Employees/Agents
- **john.manager** - john.manager@localhost.dev (Manager)
- **sarah.agent** - sarah.agent@localhost.dev (Support)
- **mike.specialist** - mike.specialist@localhost.dev (Lead)
- **lisa.support** - lisa.support@localhost.dev (Support)

### Customers
- **alice** - alice@example.com
- **bob** - bob@company.com

## Demo Data Included

### Work Containers (Projects)
- **Website Redesign Project** - Active project with multiple tasks

### Unassigned Work Items
- **URGENT: Login system not working** - Critical incident (Priority: 95)
- **Feature request: Add dark mode** - Enhancement request (Priority: 12)

### Knowledge Base Articles
- How to Reset Your Password
- Troubleshooting Common Login Issues
- Getting Started Guide

## Domains Configured

1. **Support** (Default)
   - Customer Support and service requests
   - Ticket types: Incident, Service Request, Question
   - Workflow: New → In Progress → Pending → Resolved → Closed

2. **IT**
   - Technical support and infrastructure
   - Ticket types: Incident, Service Request, Change Request
   - Workflow: Pending → In Progress → Testing → Completed

3. **Development**
   - Software development and bug tracking
   - Issue types: Bug, Feature, Task
   - Workflow: Backlog → To Do → In Progress → Code Review → Done

## GERDA AI Features

The demo includes GERDA AI system user that provides:
- Sentiment analysis
- Automatic routing
- Policy validation
- Auto-escalation for critical issues

## Reverting to Azure Configuration

To switch back to your Azure configuration:

```bash
# Restore from backup
cp seed_data.azure.json.bak seed_data.json
cp masala_domains.azure.yaml.bak masala_domains.yaml
```

## Notes

- All emails use `.dev` or common example domains
- Phone numbers use placeholder format
- No real Azure/production credentials included
- Safe for public demos and localhost testing
- GERDA AI features work with placeholder OpenAI keys (limited functionality)
