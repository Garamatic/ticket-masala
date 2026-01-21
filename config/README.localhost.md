# Localhost Demo Configuration for Ticket Masala

This directory contains generic configuration files for running Ticket Masala on localhost for demo purposes, without any Azure-specific dependencies.

## Quick Start (Easiest Method)

### Option 1: Swap to Localhost Config

```bash
cd /home/juan/Projects/garamatic/ticket-masala/config

# Backup your Azure configs (if you haven't already)
cp seed_data.json seed_data.azure.backup.json
cp masala_domains.yaml masala_domains.azure.backup.yaml

# Use localhost configs
cp seed_data.localhost.json seed_data.json
cp masala_domains.localhost.yaml masala_domains.yaml
```

Then restart your app:
```bash
cd /home/juan/Projects/garamatic/ticket-masala/src/TicketMasala.Web
dotnet run
```

### Option 2: Use Environment Variable (Recommended)

Set the config path to use localhost files without overwriting:

```bash
export MASALA_CONFIG_PATH="/home/juan/Projects/garamatic/ticket-masala/config"
export MASALA_SEED_FILE="seed_data.localhost.json"
export MASALA_DOMAINS_FILE="masala_domains.localhost.yaml"
```

## What's Included

### 📁 Configuration Files

- **`seed_data.localhost.json`** - Generic demo data with sample users, tickets, and knowledge base
- **`masala_domains.localhost.yaml`** - Three pre-configured domains (Support, IT, Development)
- **`masala_config.localhost.json`** - Generic app configuration with GERDA AI settings
- **`README.localhost.md`** - This file

### 👥 Demo Users

#### Admin
- **Username:** `admin`
- **Email:** `admin@localhost.dev`
- **Role:** Administrator

#### Employees/Agents
| Username | Email | Team | Role |
|----------|-------|------|------|
| `john.manager` | john.manager@localhost.dev | Support Team | Manager |
| `sarah.agent` | sarah.agent@localhost.dev | Support Team | Support |
| `mike.specialist` | mike.specialist@localhost.dev | Technical Team | Lead |
| `lisa.support` | lisa.support@localhost.dev | Customer Service | Support |

#### Customers
| Username | Email | Name |
|----------|-------|------|
| `customer.alice` | alice@example.com | Alice Johnson |
| `customer.bob` | bob@company.com | Bob Smith |

#### System Users
- **GERDA AI** - `gerda@ticketmasala.ai` (AI system agent)

### 🎯 Demo Data Included

#### Work Containers (Projects)
- **Website Redesign Project** - Active project with 2 in-progress tasks

#### Unassigned Work Items
1. **URGENT: Login system not working** (Priority: 95, Critical)
   - Demonstrates GERDA's auto-escalation for critical incidents
   - Includes sentiment analysis and SLA tracking

2. **Feature request: Add dark mode** (Priority: 12, Enhancement)
   - Lower priority enhancement request
   - Shows standard ticket workflow

#### Knowledge Base Articles
1. How to Reset Your Password
2. Troubleshooting Common Login Issues
3. Getting Started Guide

## 🎨 Domains Configured

### 1. Support (Default)
**Use Case:** Customer Support and service requests

- **Entity Labels:** Ticket, Project, Agent
- **Ticket Types:**
  - 🔥 Incident (Red)
  - ⚙️ Service Request (Blue)
  - ❓ Question (Yellow)
- **Workflow:** New → In Progress → Pending → Resolved → Closed

### 2. IT
**Use Case:** Technical support and infrastructure

- **Entity Labels:** Ticket, Project, Technician
- **Ticket Types:**
  - 🔥 Incident (Red)
  - ⚙️ Service Request (Blue)
  - 🔄 Change Request (Green)
- **Workflow:** Pending → In Progress → Testing → Completed

### 3. Development
**Use Case:** Software development and bug tracking

- **Entity Labels:** Issue, Sprint, Developer
- **Issue Types:**
  - 🐛 Bug (Red)
  - ⭐ Feature (Green)
  - ✅ Task (Blue)
- **Workflow:** Backlog → To Do → In Progress → Code Review → Done

## 🤖 GERDA AI Features

The demo includes GERDA AI system user with:

- ✅ **Spam Detection** - Filters out spam and invalid requests
- ✅ **Complexity Estimation** - Estimates effort points for tickets
- ✅ **Ranking** - Prioritizes tickets based on urgency and impact
- ✅ **Dispatching** - Routes tickets to appropriate agents
- ❌ **Anticipation** - Disabled (requires advanced AI setup)

> **Note:** GERDA features work with placeholder OpenAI keys but with limited functionality. For full AI capabilities, configure a valid OpenAI API key in `appsettings.json`.

## 🔄 Reverting to Azure Configuration

To switch back to your Azure/production configuration:

```bash
cd /home/juan/Projects/garamatic/ticket-masala/config

# Restore from backup
cp seed_data.azure.backup.json seed_data.json
cp masala_domains.azure.backup.yaml masala_domains.yaml
```

## 🎭 Inspired by Existing Tenants

This localhost config is designed to be a neutral, generic version that can work as a fallback if your Azure deployments have issues. It's inspired by your existing tenant themes:

- **Desgoffe** (Government) - Formal, bureaucratic
- **Hennessey** (Grants) - Academic, thorough
- **Liberty** (Tech) - Technical, concise
- **Whitman** (Industrial) - Safety-focused, high-contrast

The localhost version uses a balanced, professional tone suitable for general demos.

## 🚀 Testing the Setup

After switching to localhost config:

1. **Start the application:**
   ```bash
   cd /home/juan/Projects/garamatic/ticket-masala/src/TicketMasala.Web
   dotnet run
   ```

2. **Access the app:**
   - Open browser to `http://localhost:5000` (or whatever port is shown)
   - Login with one of the demo users
   - Explore the different domains using the domain switcher

3. **Test GERDA AI:**
   - Create a new ticket with urgent language
   - Watch GERDA auto-analyze sentiment and priority
   - Check auto-routing to appropriate team

## 📝 Notes

- ✅ All emails use `.dev` or common example domains
- ✅ Phone numbers use placeholder format
- ✅ No real Azure/production credentials included
- ✅ Safe for public demos and localhost testing
- ✅ Database uses SQLite by default (`app.db`)
- ⚠️ GERDA AI features require OpenAI API key for full functionality

## 🐛 Troubleshooting

### Database Issues
If you see database errors, delete the existing database:
```bash
rm /home/juan/Projects/garamatic/ticket-masala/src/TicketMasala.Web/app.db
```
The app will recreate it with seed data on next run.

### Config Not Loading
Ensure the config files are in the correct location:
```bash
ls -la /home/juan/Projects/garamatic/ticket-masala/config/
```

You should see:
- `seed_data.localhost.json`
- `masala_domains.localhost.yaml`
- `masala_config.localhost.json`
