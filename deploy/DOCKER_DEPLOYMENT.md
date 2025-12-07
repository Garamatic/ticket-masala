# Docker Deployment Guide

## Quick Start

### 1. Build and Run

```bash
# Build the Docker image
docker compose build

# Start the container
docker compose up -d

# View logs
docker compose logs -f
```

### 2. Directory Structure

The deployment uses the "Brain in a Box" pattern:

```
ticket-masala/
├── Dockerfile                 # Multi-stage build definition
├── docker-compose.yml         # Container orchestration
├── config/                    # Configuration volume (read-only)
│   └── masala_domains.yaml   # Domain rules and settings
└── data/                      # Data volume (read-write)
    └── masala.db             # SQLite database (auto-created)
```

### 3. First Time Setup

Create the required directories:

```bash
mkdir -p config data
chmod 755 config
chmod 777 data  # Needs write access for SQLite
```

### 4. Configuration Hot-Reload

Edit `config/masala_domains.yaml` to change rules. The application will automatically detect changes and reload without restart.

### 5. Accessing the Application

- **Web UI**: <http://localhost:8080>
- **Health Check**: <http://localhost:8080/health> (if implemented)

### 6. Troubleshooting

**Permission Denied on masala.db:**

```bash
sudo chown -R 1000:1000 ./data
```

**Config file not found:**
Ensure `config/masala_domains.yaml` exists before starting.

**Container won't start:**

```bash
docker compose logs ticket-masala
```

### 7. Production Deployment

For production, update `docker-compose.yml`:

- Change `ASPNETCORE_ENVIRONMENT` to `Production`
- Use secrets management for sensitive data
- Mount volumes to persistent storage (not local directories)

### 8. Stopping the Application

```bash
docker compose down
```

To remove volumes as well:

```bash
docker compose down -v
```
