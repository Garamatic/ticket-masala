# Development Guide

Complete guide for local development of Ticket Masala.

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **IDE** - Visual Studio 2022, VS Code with C# Dev Kit, or JetBrains Rider
- **Git** - Version control

Optional:
- **Docker** - For containerized development
- **SQLite Browser** - For database inspection (DB Browser for SQLite)

---

## Quick Start

```bash
# Clone repository
git clone https://github.com/your-org/ticket-masala.git
cd ticket-masala

# Navigate to project
cd src/factory/TicketMasala

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (creates database on first run)
dotnet run --project src/TicketMasala.Web/

# Open browser
# http://localhost:5054
```

---

## Project Structure

```
TicketMasala/
├── src/
│   ├── TicketMasala.Web/          # Main ASP.NET Core application
│   │   ├── Areas/                 # MVC areas (Admin, Identity)
│   │   ├── Controllers/           # MVC + API controllers
│   │   ├── Engine/                # Business logic
│   │   │   ├── Core/              # Ticket, Notification services
│   │   │   ├── GERDA/             # AI modules
│   │   │   ├── Compiler/          # Rule engine
│   │   │   └── Ingestion/         # CSV/Email import
│   │   ├── Extensions/            # DI registration extensions
│   │   ├── Middleware/            # Custom middleware
│   │   ├── Observers/             # Event handlers
│   │   ├── Repositories/          # Data access
│   │   ├── ViewModels/            # DTO models
│   │   └── Views/                 # Razor views
│   ├── TicketMasala.Domain/       # Domain entities
│   ├── TicketMasala.Tests/        # Test project
│   └── GatekeeperApi/             # Ingestion service
├── tenants/                       # Tenant configurations
├── config/                        # Default configuration
└── docs/                          # Documentation
```

---

## Configuration

### Environment Variables

```bash
# Configuration directory (default: ./config in development)
export MASALA_CONFIG_PATH=/path/to/config

# Plugin directory for tenant extensions
export MASALA_PLUGINS_PATH=/path/to/plugins

# Database connection (SQLite by default)
export ConnectionStrings__DefaultConnection="Data Source=app.db"
```

### Configuration Files

Located in `config/` directory:

| File | Purpose |
|------|---------|
| `masala_config.json` | GERDA AI settings, feature flags |
| `masala_domains.yaml` | Domain workflows, custom fields |
| `seed_data.json` | Initial database data |

See [Configuration Guide](CONFIGURATION.md) for details.

---

## Database Management

### First Run

The database is created and seeded automatically on first run.

### Reset Database

```bash
# Delete database file
rm -f src/TicketMasala.Web/app.db*

# Restart application
dotnet run --project src/TicketMasala.Web/
```

### Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName \
  --project src/TicketMasala.Web \
  --context MasalaDbContext

# Apply migrations
dotnet ef database update \
  --project src/TicketMasala.Web

# Generate SQL script
dotnet ef migrations script \
  --project src/TicketMasala.Web \
  --output migration.sql
```

### Inspect Database

```bash
# SQLite CLI
sqlite3 src/TicketMasala.Web/app.db

# Common queries
.tables                          # List tables
.schema Tickets                  # Show table schema
SELECT * FROM Tickets LIMIT 5;   # Query data
```

---

## Running the Application

### Development Server

```bash
# Standard development
dotnet run --project src/TicketMasala.Web/

# With hot reload
dotnet watch run --project src/TicketMasala.Web/

# Specific port
dotnet run --project src/TicketMasala.Web/ --urls "http://localhost:5000"
```

### With Docker

```bash
# Build and run
docker-compose up --build

# Run specific tenant
docker-compose up government

# Build only
docker build -t ticket-masala .
```

---

## Application Startup Flow

1. **Configuration Loading** - `AddMasalaConfiguration()`
2. **Plugin Loading** - `TenantPluginLoader.LoadPlugins()`
3. **Database Setup** - `AddMasalaDatabase()`
4. **Identity Configuration** - `AddMasalaIdentity()`
5. **Repository Registration** - `AddRepositories()`
6. **Observer Registration** - `AddObservers()`
7. **Core Services** - `AddCoreServices()`
8. **GERDA AI Services** - `AddGerdaServices()`
9. **Middleware Pipeline** - `UseMasalaCore()`
10. **Service Initialization** - `InitializeMasalaCoreAsync()`

---

## Test Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@ticketmasala.com | Admin123! |
| Employee | mike.pm@ticketmasala.com | Employee123! |
| Customer | alice.customer@example.com | Customer123! |

---

## Debugging

### VS Code

```json
// .vscode/launch.json
{
  "configurations": [
    {
      "name": ".NET Core Launch",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/TicketMasala.Web/bin/Debug/net10.0/TicketMasala.Web.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/TicketMasala.Web",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Logging

Configure in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TicketMasala": "Debug",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## Common Development Tasks

### Add a New Controller

```csharp
// Controllers/NewFeatureController.cs
[Authorize]
public class NewFeatureController : Controller
{
    private readonly IFeatureService _service;
    
    public NewFeatureController(IFeatureService service)
    {
        _service = service;
    }
    
    public IActionResult Index() => View();
}
```

### Add a New Service

```csharp
// Engine/NewFeature/INewFeatureService.cs
public interface INewFeatureService
{
    Task<Result> DoSomethingAsync();
}

// Engine/NewFeature/NewFeatureService.cs
public class NewFeatureService : INewFeatureService
{
    public async Task<Result> DoSomethingAsync()
    {
        // Implementation
    }
}

// Register in Extensions/ServiceCollectionExtensions.cs
services.AddScoped<INewFeatureService, NewFeatureService>();
```

### Add a New Observer

```csharp
// Observers/NewTicketObserver.cs
public class NewTicketObserver : ITicketObserver
{
    public async Task OnTicketCreatedAsync(Ticket ticket)
    {
        // React to ticket creation
    }
}

// Register in Extensions/ObserverExtensions.cs
services.AddScoped<ITicketObserver, NewTicketObserver>();
```

---

## Tenant Development

### Create New Tenant

1. Copy template:
   ```bash
   cp -r tenants/_template tenants/my-tenant
   ```

2. Configure `tenants/my-tenant/config/masala_domains.yaml`

3. Update `docker-compose.yml`:
   ```yaml
   my-tenant:
     <<: *base-service
     ports:
       - "8085:8080"
     volumes:
       - ./tenants/my-tenant:/app/tenant-config
   ```

4. Run:
   ```bash
   docker-compose up my-tenant
   ```

---

## Performance Profiling

```bash
# Enable detailed timing
export ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug

# Use dotnet-trace
dotnet tool install -g dotnet-trace
dotnet-trace collect --process-id <PID>

# Use dotnet-counters
dotnet tool install -g dotnet-counters
dotnet-counters monitor --process-id <PID>
```

---

## Further Reading

- [Testing Guide](TESTING.md)
- [Configuration Guide](CONFIGURATION.md)
- [Architecture Overview](../architecture/SUMMARY.md)
- [API Reference](../api/API_REFERENCE.md)
