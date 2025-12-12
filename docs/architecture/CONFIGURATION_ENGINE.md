This is the heartbeat of the system. If the ConfigurationService fails, the engine is brainless.

We are implementing a Hot-Reloadable, Versioned Configuration Engine.
The Flow: "Watch, Hash, Snapshot"

We don't just read the YAML; we version it. Every time you edit the file:

    Watch: We detect the file change.

    Hash: We compute a checksum of the new content.

    Snapshot: If this version is new, we save it to SQLite (DomainConfigVersions table).

    Hot Swap: We update the in-memory singleton so the next ticket uses the new rules instantly.

Code snippet

sequenceDiagram
    participant Dev as Developer
    participant FS as FileSystem
    participant Svc as ConfigService
    participant DB as SQLite
    participant Mem as Memory Cache

    Dev->>FS: Edit masala_domains.yaml
    FS->>Svc: OnChanged Event
    Svc->>Svc: Calculate MD5 Hash
    Svc->>DB: Check if Hash exists
    alt Is New Version
        Svc->>DB: INSERT Snapshot (ID: v2, JSON: {...})
    end
    Svc->>Mem: Update CurrentConfig Singleton
    Note right of Mem: Next Ticket uses v2 automatically

Step 1: The Configuration POCOs

First, we define what our YAML maps to. Create a new folder: src/TicketMasala.Web/Engine/Configuration/Models.

DomainConfiguration.cs This represents the structure of your masala_domains.yaml.
C#

namespace TicketMasala.Web.Engine.Configuration.Models;

public class RootConfiguration
{
    public List<DomainDefinition> Domains { get; set; } = new();
    public GlobalSettings Settings { get; set; } = new();
}

public class DomainDefinition
{
    public string Id { get; set; } // "IT", "HR"
    public string Name { get; set; }

    // Defines what fields are available for this domain
    public List<FieldDefinition> Fields { get; set; } = new();
    
    // Defines the workflow states
    public List<string> States { get; set; } = new();
    
    // Defines rules for transitions (e.g., "Auto-Approve if < $500")
    public List<RuleDefinition> Rules { get; set; } = new();
}

public class FieldDefinition
{
    public string Key { get; set; } // "priority_score"
    public string Type { get; set; } // "Number", "String", "Select"
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
}

public class RuleDefinition
{
    public string Name { get; set; }
    public string Condition { get; set; } // "ticket.priority_score > 10"
    public string Action { get; set; } // "TransitionTo:HighPriority"
}

public class GlobalSettings
{
    public int MaxConcurrentWorkers { get; set; } = 5;
}

Step 2: The Persistence Entity (Snapshot)

We need to store history. If a ticket was processed 6 months ago, we need to know what the rules were then.

Add this to src/TicketMasala.Web/Domain/Entities/DomainConfigVersion.cs:
C#

using System.ComponentModel.DataAnnotations;
using TicketMasala.Web.Data.Entities; // Assuming MasalaEntity is here

namespace TicketMasala.Web.Domain.Entities;

public class DomainConfigVersion : MasalaEntity
{
    [Required]
    [MaxLength(64)]
    public string Hash { get; set; } // MD5/SHA256 of the YAML content

    [Required]
    public string YamlContent { get; set; } // The raw backup

    public bool IsActive { get; set; }
}

(Don't forget to add DbSet<DomainConfigVersion> to your MasalaDbContext later).
Step 3: The Engine Service

Create src/TicketMasala.Web/Engine/Configuration/MasalaConfigService.cs.

This service is a Singleton. It holds the state for the entire application.
C#

using System.Security.Cryptography;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using TicketMasala.Web.Engine.Configuration.Models;
using TicketMasala.Web.Data;
using TicketMasala.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Engine.Configuration;

public interface IMasalaConfigService
{
    RootConfiguration Current { get; }
    string CurrentVersionId { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
}

public class MasalaConfigService : IMasalaConfigService, IDisposable
{
    private readonly string _configPath;
    private readonly ILogger<MasalaConfigService>_logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private FileSystemWatcher?_watcher;

    // The "Live" Config
    public RootConfiguration Current { get; private set; } = new();
    public string CurrentVersionId { get; private set; } = string.Empty;

    public MasalaConfigService(IConfiguration config, ILogger<MasalaConfigService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        
        // Read path from Env/appsettings, default to /app/config
        _configPath = config["MasalaConfig:ConfigPath"] ?? "/app/config";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_configPath, "masala_domains.yaml");
        
        if (!File.Exists(filePath))
        {
            _logger.LogCritical($"Config file not found at {filePath}");
            throw new FileNotFoundException("masala_domains.yaml missing");
        }

        await LoadConfigAsync(filePath);
        StartWatching(filePath);
    }

    private async Task LoadConfigAsync(string filePath)
    {
        try 
        {
            // 1. Read File
            // We use a retry policy here because FileSystemWatcher often fires while the file is still locked by the writer.
            string yaml = await ReadFileWithRetryAsync(filePath);
            
            // 2. Compute Hash
            var hash = ComputeHash(yaml);

            // 3. Deserialize (Fail fast if invalid)
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var newConfig = deserializer.Deserialize<RootConfiguration>(yaml);

            // 4. Persist Snapshot (Database Interaction)
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>(); // Rename to your actual DbContext class
                
                // Check if we've seen this version before
                var existing = await db.DomainConfigVersions
                    .FirstOrDefaultAsync(v => v.Hash == hash);

                if (existing == null)
                {
                    _logger.LogInformation($"New Configuration Detected (Hash: {hash}). Snapshotting...");
                    var version = new DomainConfigVersion 
                    { 
                        Hash = hash, 
                        YamlContent = yaml,
                        IsActive = true 
                    };
                    db.DomainConfigVersions.Add(version);
                    await db.SaveChangesAsync();
                    CurrentVersionId = version.Id;
                }
                else
                {
                    _logger.LogInformation($"Restored known configuration version: {existing.Id}");
                    CurrentVersionId = existing.Id;
                }
            }

            // 5. Atomic Swap
            Current = newConfig;
            _logger.LogInformation("Configuration Reloaded Successfully.");
            
            // TODO: Trigger Event for RuleCompiler to re-compile expressions!
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration. Keeping previous version active.");
        }
    }

    private void StartWatching(string fullPath)
    {
        var directory = Path.GetDirectoryName(fullPath);
        var filename = Path.GetFileName(fullPath);

        _watcher = new FileSystemWatcher(directory!)
        {
            Filter = filename,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };

        _watcher.Changed += async (s, e) => await LoadConfigAsync(fullPath);
        _watcher.Created += async (s, e) => await LoadConfigAsync(fullPath);
        _watcher.EnableRaisingEvents = true;
        
        _logger.LogInformation($"Watching for config changes in {directory}...");
    }

    private static string ComputeHash(string content)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = md5.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
    
    // Helper to handle file locking during edits
    private async Task<string> ReadFileWithRetryAsync(string path)
    {
        for (int i = 0; i < 3; i++)
        {
            try { return await File.ReadAllTextAsync(path); }
            catch (IOException) { await Task.Delay(500); }
        }
        return await File.ReadAllTextAsync(path);
    }

    public void Dispose() => _watcher?.Dispose();
}

Step 4: Wire it up in Program.cs

We need to install the YAML parser first:
Bash

dotnet add src/TicketMasala.Web/TicketMasala.Web.csproj package YamlDotNet

Then, modify Program.cs to initialize this before the app starts serving traffic.
C#

// In Program.cs

// 1. Register as Singleton
builder.Services.AddSingleton<IMasalaConfigService, MasalaConfigService>();

var app = builder.Build();

// 2. Initialize Logic (The "Warm Up")
using (var scope = app.Services.CreateScope())
{
    // Ensure DB is created (and Schema exists)
    var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>(); // Your Context
    db.Database.Migrate();

    // Load the Config
    var configService = scope.ServiceProvider.GetRequiredService<IMasalaConfigService>();
    await configService.InitializeAsync(CancellationToken.None);
}

// ... rest of pipeline

5. The Critical Dependency: masala_domains.yaml

Since the service crashes if the file is missing, you must create a default file in your local config/ folder.

config/masala_domains.yaml (Reference Content):
YAML

settings:
  maxConcurrentWorkers: 5

domains:

- id: "IT"
    name: "Information Technology"
    states: ["New", "Triaged", "In Progress", "Done"]
    fields:
  - key: "priority_score"
        type: "Number"
        defaultValue: "0"
  - key: "os_version"
        type: "String"
    rules:
  - name: "AutoEscalate"
        condition: "priority_score > 80"
        action: "SetPriority:Critical"
