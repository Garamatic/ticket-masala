# Ecosystem Deployment Guide

Ticket Masala is part of a larger multi-tenant ecosystem orchestrated by the `masala-web` project. This guide explains how the deployment pipeline works.

## Architecture

The `masala-web` repository acts as the **Deployment Controller**. It contains the configuration for deploying:

1. **Marketing Site**: The main landing page (`masala-web`).
2. **Documentation**: This MkDocs site (`ticket-masala/docs`).
3. **Garamatic Web**: The parent company site (`garamatic-web`).
4. **Tenant Portals**: Individual instances of Ticket Masala for demo tenants (Desgoffe, Whitman, Liberty, Hennessey).

### Directory Structure

All deployment configurations are centralized in the `deploy/` directory of the `masala-web` repository:

```text
masala-web/
├── deploy/
│   ├── deploy-all.fish       # Master deployment script
│   ├── fly.toml              # Marketing site config
│   ├── fly.desgoffe-api.toml # Tenant API config
│   ├── fly.desgoffe-portal.toml # Tenant Portal config
│   └── Dockerfile.tenant     # Tenant Portal Dockerfile
├── tenants/                  # Tenant-specific assets (themes, logos)
└── src/                      # Marketing site source code
```

## Running Deployments

To deploy the entire ecosystem or specific parts, use the `deploy-all.fish` script located in `masala-web/deploy/`.

### Prerequisites

- [Fly.io CLI](https://fly.io/docs/flyctl/install/) installed and authenticated.
- `fish` shell (or read the script to run commands manually).

### Command

```bash
# From the masala-web/deploy directory:
./deploy-all.fish
```

> [!NOTE]
> The script automatically navigates to the project root to execute Docker builds with the correct context.

## Tenant Deployment Process

The deployment script performs a sophisticated "Staged Injection" process for each tenant:

1. **Staging**: Creates a temporary directory `/tmp/masala-build-<tenant>`.
2. **Copy Core**: Copies the core `ticket-masala` backend code.
3. **Inject Theme**: Copies tenant-specific CSS and Logos from `masala-web/tenants/<tenant>/theme` into the build context.
4. **Patch Dockerfile**: Modifies the Dockerfile to bake the specific theme into the container image.
5. **Deploy**: Pushes the customized image to Fly.io using the specific `fly.<tenant>-api.toml` configuration.

This ensures that while all tenants run the same core code, they have distinct visual identities and branding baked into their artifacts.
