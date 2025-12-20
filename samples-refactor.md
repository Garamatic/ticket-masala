This is **ATOM**. I see the blueprint.

You currently have a "Single-Purpose Workshop." You have built a machine (`TicketMasala.Web`) that is slightly hardwired to the materials on the floor (`root/config` and `root/data`).

To achieve your goal—running distinct "Client Molds" through the same "Factory Line"—we must decouple the **Machinery** (Code/Container) from the **Specification** (Config/Theme).

We are moving to a **Multi-Tenant Injection** model. The Container is the engine; the "Sample" folder is the fuel cartridge.

Here is the architectural restructure plan.

-----

### 1\. The Repository Restructure (The Warehouse)

We need to clear the factory floor. The root directory is cluttered with "default" configuration data. We will move all specific configurations into a standardized `tenants` directory.

**Current State:** Mixed concerns.
**Target State:** Clean separation.

```text
/ (Root)
├── src/                    <-- THE MACHINE (Immutable Code)
├── tenants/                <-- THE MOLDS (Interchangeable Configs)
│   ├── _template/          <-- The "Base" Mold
│   ├── default/            <-- Moved from your current root config/data
│   ├── government/         <-- Renamed from samples/government-services
│   ├── healthcare/         <-- Renamed from samples/healthcare-clinic
│   └── helpdesk/           <-- Renamed from samples/it-helpdesk
├── docker-compose.yml      <-- The Production Line Manager
└── Dockerfile              <-- The Blueprint
```

**The "Tenant Protocol" (Standardized Folder Structure):**
Every folder inside `tenants/` must strictly adhere to this interface so the Factory can ingest it without errors:

```text
tenants/healthcare/
├── config/
│   ├── masala_config.json
│   └── masala_domains.yaml
├── theme/
│   ├── style.css
│   └── logo.png
└── data/
    └── seed_data.json
```

### 2\. The Factory Refit (Dockerfile Update)

We must teach the container to stop looking at its own feet for configuration and instead look at a **Designated Input Slot**.

Your app likely looks for `./config/masala_config.json`. We will enforce that the container *always* expects configuration at `/app/config_mount`.

**Action:** Update your application code (Configuration Builder) to prioritize this path if present, or set an Environment Variable `MASALA_CONFIG_PATH`.

### 3\. The Orchestrator (Docker Compose)

We will use **Docker Compose Profiles** or simply multiple service definitions to run the factory with different inputs simultaneously.

**New `docker-compose.yml` Pattern:**

```yaml
services:
  # The Machine
  app-core: &app-base
    image: ticket-masala:latest
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - CONFIG_PATH=/app/inputs/config  <-- The Internal Input Slot
    volumes:
      # We mount the specific "Mold" into the "Input Slot"
      - ./tenants/default:/app/inputs

  # Tenant 1: Government
  gov-instance:
    <<: *app-base
    ports:
      - "8081:8080"
    volumes:
      - ./tenants/government:/app/inputs

  # Tenant 2: Healthcare
  health-instance:
    <<: *app-base
    ports:
      - "8082:8080"
    volumes:
      - ./tenants/healthcare:/app/inputs
```

### 4\. Detailed Migration Steps

Here is how we execute this refactor without breaking the build:

1.  **Create the Directory:** `mkdir tenants`
2.  **Migrate Default:**
      * `mkdir -p tenants/default`
      * Move `config/`, `data/`, and `roocode.sqlite` into `tenants/default/`.
3.  **Migrate Samples:**
      * Move `samples/government-services` to `tenants/government`.
      * *Crucial:* Reorganize the internal file structure of `tenants/government` to match `tenants/default` (e.g., ensure it has a `config/` subfolder, not just `config.json` at the root).
4.  **Update .NET Core `Program.cs`:**
      * Ensure the `ConfigurationBuilder` adds JSON files from the path defined in the environment variable (e.g., `/app/inputs/config`).

-----

### Architect's Note on "The UI"

You have `index.html` and `main.js` in your sample folders. If your app is a .NET MVC/Blazor app (`TicketMasala.Web`), these static HTML files might be bypassed unless you are serving them strictly as static assets.

  * **If you use Blazor/Razor:** The "Theme" folder should only contain CSS/Images. The logic stays in the DLL.
  * **If you use the HTML as a SPA:** You need to mount `tenants/government/index.html` to `/app/wwwroot/index.html`.

### IMMEDIATE NEXT STEP

I can generate the shell script to automate this file reorganization (creating the `tenants` folder and moving the `samples` into the standardized structure) so you don't have to drag-and-drop manually.

**Shall I deploy the `refactor_tenants.sh` script?**