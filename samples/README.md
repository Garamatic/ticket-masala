# TicketMasala Sample Instances

This directory contains domain-specific client implementations that demonstrate the versatility of the TicketMasala platform across different industries and use cases.

## Available Samples

| Sample | Domain | Description | Theme |
|--------|--------|-------------|-------|
| `it-helpdesk` | IT Support | Corporate IT support desk | Blue/Gray Professional |
| `landscaping-client` | Gardening | Landscaping quote requests | Green/Gold Nature |
| `government-services` | Government | Municipal citizen services | Burgundy/Gold Formal |
| `healthcare-clinic` | Healthcare | Medical appointment system | Teal/White Medical |

## Quick Start

### Running a Sample

```bash
cd samples/[sample-name]
python3 -m http.server 8000
# Open http://localhost:8000
```

### Connecting to Backend

Samples connect to the TicketMasala.Web API. Ensure the backend is running:

```bash
cd src/TicketMasala.Web
dotnet run
# API available at http://localhost:5054
```

## Architecture

Each sample follows a standardized structure:

```
sample-name/
├── config.json          # Configuration (client, masala, gerda)
├── index.html           # Landing page
├── style.css            # Theme-specific styles
├── main.js              # Shared JavaScript logic
├── seed_data.json       # Domain-specific sample tickets (optional)
└── assets/              # Images and media
    └── thumbnails/      # Project/service thumbnails
```

## Configuration Schema

Each `config.json` contains three sections:

### `client_config`
Domain-specific branding and content:
- `company`: Name, slogan, logo, description
- `theme`: Color palette (primaryColor, secondaryColor, accentColor)
- `services`: Available service types
- `projects`: Portfolio/showcase items
- `contact`: Contact information
- `form`: Form configuration

### `masala_config`
Backend API configuration:
- `endpoint`: API URL for ticket submission
- `sourceSite`: Identifier for this client
- `auth`: Authentication settings

### `gerda_config`
Domain-specific validation and workflow rules:
- `domain`: Maps to `masala_domains.yaml` domain
- `validationRules`: Field validation patterns
- `workflow`: Default priority and assignment rules

## Creating a New Sample

1. **Copy the Template**
   ```bash
   cp -r _template/ my-new-sample/
   ```

2. **Configure `config.json`**
   - Set company branding
   - Define theme colors
   - Configure services and projects
   - Set API endpoint

3. **Customize Styles**
   - Update CSS variables in `style.css`
   - Add domain-specific styling

4. **Add Assets**
   - Add logo, thumbnails, and imagery
   - Use consistent naming conventions

5. **Register Domain** (optional)
   - Add domain definition to `config/masala_domains.yaml`
   - Define work item types and workflows

## Theme Color System

Themes use CSS custom properties for easy customization:

```css
:root {
    --primary: #2d5a27;       /* Main brand color */
    --primary-dark: #1e3d1a;  /* Darker variant */
    --primary-light: #4a7c43; /* Lighter variant */
    --accent: #c9a227;        /* Call-to-action color */
    --accent-light: #e5c75f;  /* Light accent */
}
```

The `main.js` automatically applies theme colors from `config.json` to CSS variables on page load.

## Powered by TicketMasala

All samples connect to the [TicketMasala](../README.md) platform for:
- 🎫 Ticket/request management
- 🤖 AI-powered routing (GERDA)
- 📊 Analytics and reporting
- 🔄 Workflow automation
