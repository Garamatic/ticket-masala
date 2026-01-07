# Ticket Masala - Production Deployment Guide

This guide details how to deploy Ticket Masala to a production environment.

## 1. Prerequisites

- **Runtime:** .NET 10.0 Runtime (or SDK for building)
- **OS:** Linux (Ubuntu 22.04+ recommended) or Windows Server 2022+
- **Container:** Docker Engine (optional, but recommended)
- **Reverse Proxy:** Nginx, Traefik, or IIS

## 2. Configuration & Environment Variables

Ticket Masala is a **configuration-driven** application. The following environment variables control its behavior:

| Variable                               | Description                   | Default / Example                 |
| -------------------------------------- | ----------------------------- | --------------------------------- |
| `ASPNETCORE_ENVIRONMENT`               | Application mode              | `Production`                      |
| `MASALA_CONFIG_PATH`                   | Path to `masala_domains.yaml` | `/app/config/masala_domains.yaml` |
| `ConnectionStrings__DefaultConnection` | Database connection string    | `Data Source=/app/data/masala.db` |
| `Serilog__MinimumLevel`                | Logging verbosity             | `Information`                     |

### Domain Configuration

The `masala_domains.yaml` file defines the active tenants (e.g., Desgoffe, Liberty). Ensure this file is mounted or copied to the location specified by `MASALA_CONFIG_PATH`.

## 3. Deployment Options

### Option A: Docker (Recommended)

Use the provided `Dockerfile` in the root of the project.

**1. Build the Image:**

```bash
docker build -t ticket-masala:latest -f src/TicketMasala.Web/Dockerfile .
```

**2. Run the Container:**

```bash
docker run -d \
  --name ticket-masala \
  -p 8080:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/config:/app/config \
  -e MASALA_CONFIG_PATH=/app/config/masala_domains.yaml \
  ticket-masala:latest
```

### Option B: Manual (Linux/Kestrel)

**1. Publish the App:**

```bash
dotnet publish src/TicketMasala.Web/TicketMasala.Web.csproj -c Release -o ./publish
```

**2. Copy Files:**
Transfer the `./publish` directory to your server (e.g., `/var/www/ticket-masala`).

**3. Set Up Service (Systemd):**
Create `/etc/systemd/system/ticket-masala.service`:

```ini
[Unit]
Description=Ticket Masala Web App

[Service]
WorkingDirectory=/var/www/ticket-masala
ExecStart=/usr/bin/dotnet /var/www/ticket-masala/TicketMasala.Web.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=ticket-masala
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

**4. Start Service:**

```bash
sudo systemctl enable ticket-masala
sudo systemctl start ticket-masala
```

## 4. Database Considerations

### SQLite (Default)

Ticket Masala defaults to SQLite. For production:

- Ensure the **directory** containing the `.db` file is writable by the application user.
- **WAL Mode:** The application automatically enables Write-Ahead Logging (WAL) for better concurrency.
- **Backup:** Use `sqlite3 .backup` command or volume snapshots. Do NOT just copy the active `.db` file while the app is writing.

### SQL Server (Optional)

To use SQL Server, update your `appsettings.json` or environment variable:
`ConnectionStrings__DefaultConnection="Server=myServer;Database=TicketMasala;User Id=sa;..."`

## 5. Security Checklist

- [ ] **HTTPS:** Always terminate SSL at your reverse proxy (Nginx/Traefik).
- [ ] **Secret Management:** Do not commit connection strings to git. Use User Secrets or Environment Variables.
- [ ] **Data Protection:** Persist Data Protection keys if using a cluster (e.g., to Redis or a shared path).

---

_For further architectural details, refer to the [Architectural Deep Dive](https://masala-doc.fly.dev)._
