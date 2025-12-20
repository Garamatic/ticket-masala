# Tenants Directory

This directory contains tenant-specific configurations for running multiple instances of Ticket Masala with different branding, domains, and workflows.

## Structure

Each tenant folder follows the **Tenant Protocol**:

```
tenants/<tenant-name>/
├── config/                    # Backend configuration
│   ├── masala_config.json     # GERDA AI settings
│   └── masala_domains.yaml    # Domain workflows & rules
├── theme/                     # Visual customization
│   └── style.css              # Custom CSS styling
├── data/                      # Persistent data
│   └── seed_data.json         # Initial seed data
└── client/                    # SPA frontend (optional)
    ├── index.html             # Landing page
    ├── main.js                # Client logic
    └── config.json            # Client-side configuration
```

## Available Tenants

| Tenant | Port | Description |
|--------|------|-------------|
| `default` | 8080 | Base IT ticketing instance |
| `government` | 8081 | Municipal services portal |
| `healthcare` | 8088 | Medical clinic scheduling |
| `helpdesk` | 8083 | Internal IT helpdesk |
| `landscaping` | 8084 | Garden services management |

## Usage

### Start a Single Tenant

```bash
docker compose up default
docker compose up --profile government government
```

### Start All Tenants

```bash
docker compose --profile all up
```

### Development Mode

Point `MASALA_CONFIG_PATH` to a tenant's config directory:

```bash
export MASALA_CONFIG_PATH=./tenants/healthcare/config
dotnet run --project src/TicketMasala.Web
```

## Tenants Site

We provide a small static site with a landing page, demos collector, and a client SPA for submitting demo tickets.

- Landing & quick links: [tenants/site/index.html](tenants/site/index.html#L1)
- Demos list: [tenants/site/demos.html](tenants/site/demos.html#L1) — update the fly.io hosts as necessary.
- Client SPA: [tenants/site/spa/index.html](tenants/site/spa/index.html#L1) — a minimal form that POSTs to `api/tickets` by default.

See [tenants/site/README.md](tenants/site/README.md#L1) for configuration and usage notes.

## Creating a New Tenant

1. Copy `_template/` to your new tenant name
2. Update `config/masala_config.json` with instance name
3. Customize `config/masala_domains.yaml` for your domain
4. Modify `theme/style.css` with branding colors
5. Add tenant to `docker-compose.yml` following the YAML anchor pattern
