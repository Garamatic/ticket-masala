using TicketMasala.Web.Models.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using TicketMasala.Web.Engine.Compiler;

namespace TicketMasala.Web.Engine.GERDA.Configuration;

/// <summary>
/// Implementation of IDomainConfigurationService that reads from masala_domains.yaml.
/// Supports caching and automatic hot-reload on file change.
/// </summary>
public class DomainConfigurationService : IDomainConfigurationService, IDisposable
{
    private readonly ILogger<DomainConfigurationService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly RuleCompilerService _ruleCompiler;
    private MasalaDomainsConfig _config;
    private readonly object _configLock = new();
    private readonly string _configFilePath;
    private DateTime _lastLoadTime;
    private FileSystemWatcher? _fileWatcher;

    public DomainConfigurationService(
        ILogger<DomainConfigurationService> logger,
        IWebHostEnvironment environment,
        RuleCompilerService ruleCompiler)
    {
        _logger = logger;
        _environment = environment;
        _ruleCompiler = ruleCompiler;

        // Use centralized configuration path resolution
        _configFilePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "masala_domains.yaml");

        _config = new MasalaDomainsConfig();

        LoadConfiguration();
        SetupFileWatcher();
    }

    private void SetupFileWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            var fileName = Path.GetFileName(_configFilePath);

            if (directory == null) return;

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            // Debounce: only reload if file hasn't changed in 500ms
            _fileWatcher.Changed += (_, _) =>
            {
                Task.Delay(500).ContinueWith(_ => ReloadConfiguration());
            };

            _logger.LogInformation("File watcher enabled for {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not setup file watcher for hot reload");
        }
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }

    private void LoadConfiguration()
    {
        lock (_configLock)
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogWarning("Domain configuration file not found at {Path}. Using defaults.", _configFilePath);
                    _config = CreateDefaultConfig();
                    _ruleCompiler.ReplaceRuleCache(_config);
                    return;
                }

                var yaml = File.ReadAllText(_configFilePath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var newConfig = deserializer.Deserialize<MasalaDomainsConfig>(yaml) ?? CreateDefaultConfig();

                // HOT RELOAD LOGIC:
                // 1. Update local config object
                _config = newConfig;
                _lastLoadTime = DateTime.UtcNow;

                // 2. Push new config to Rule Compiler for atomic swap
                _ruleCompiler.ReplaceRuleCache(_config);

                _logger.LogInformation(
                    "Loaded domain configuration with {Count} domains: {Domains}",
                    _config.Domains.Count,
                    string.Join(", ", _config.Domains.Keys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load domain configuration from {Path}", _configFilePath);
                // Keep existing config on error
            }
        }
    }

    private static MasalaDomainsConfig CreateDefaultConfig()
    {
        return new MasalaDomainsConfig
        {
            Domains = new Dictionary<string, DomainConfig>
            {
                ["IT"] = new DomainConfig
                {
                    DisplayName = "IT Support",
                    Description = "Default IT ticketing configuration",
                    EntityLabels = new EntityLabels
                    {
                        WorkItem = "Ticket",
                        WorkContainer = "Project",
                        WorkHandler = "Agent"
                    },
                    WorkItemTypes = new List<WorkItemTypeDefinition>
                    {
                        new() { Code = "INCIDENT", Name = "Incident", Icon = "fa-fire", Color = "#dc3545", DefaultSlaDays = 1 },
                        new() { Code = "SERVICE_REQUEST", Name = "Service Request", Icon = "fa-cogs", Color = "#17a2b8", DefaultSlaDays = 5 }
                    },
                    Workflow = new WorkflowConfig
                    {
                        States = new List<WorkflowStateDefinition>
                        {
                            new() { Code = "Pending", Name = "Pending", Color = "#6c757d" },
                            new() { Code = "InProgress", Name = "In Progress", Color = "#007bff" },
                            new() { Code = "Completed", Name = "Completed", Color = "#28a745" },
                            new() { Code = "Cancelled", Name = "Cancelled", Color = "#343a40" }
                        },
                        Transitions = new Dictionary<string, List<string>>
                        {
                            ["Pending"] = new() { "InProgress", "Cancelled" },
                            ["InProgress"] = new() { "Completed", "Cancelled" },
                            ["Completed"] = new() { "InProgress" }, // Reopen
                            ["Cancelled"] = new()
                        }
                    }
                }
            },
            Global = new GlobalConfig
            {
                DefaultDomain = "IT",
                AllowDomainSwitching = false,
                ConfigReloadEnabled = true
            }
        };
    }

    public DomainConfig? GetDomain(string domainId)
    {
        return _config.Domains.TryGetValue(domainId, out var domain) ? domain : null;
    }

    public Dictionary<string, DomainConfig> GetAllDomains()
    {
        return _config.Domains;
    }

    public IEnumerable<string> GetAllDomainIds()
    {
        return _config.Domains.Keys;
    }

    public string GetDefaultDomainId()
    {
        return _config.Global.DefaultDomain;
    }

    public string GetDomainDisplayName(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.DisplayName ?? domainId;
    }

    public EntityLabels GetEntityLabels(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.EntityLabels ?? new EntityLabels();
    }

    public IEnumerable<WorkItemTypeDefinition> GetWorkItemTypes(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.WorkItemTypes ?? Enumerable.Empty<WorkItemTypeDefinition>();
    }

    public WorkItemTypeDefinition? GetWorkItemType(string domainId, string typeCode)
    {
        return GetWorkItemTypes(domainId)
            .FirstOrDefault(t => t.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<CustomFieldDefinition> GetCustomFields(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.CustomFields ?? Enumerable.Empty<CustomFieldDefinition>();
    }

    public IEnumerable<CustomFieldDefinition> GetRequiredFieldsForType(string domainId, string workItemTypeCode)
    {
        return GetCustomFields(domainId)
            .Where(f => f.Required || f.RequiredForTypes.Contains(workItemTypeCode, StringComparer.OrdinalIgnoreCase));
    }

    public IEnumerable<WorkflowStateDefinition> GetWorkflowStates(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.Workflow.States ?? Enumerable.Empty<WorkflowStateDefinition>();
    }

    public IEnumerable<string> GetValidTransitions(string domainId, string currentState)
    {
        var domain = GetDomain(domainId);
        if (domain?.Workflow.Transitions.TryGetValue(currentState, out var transitions) == true)
        {
            return transitions;
        }
        return Enumerable.Empty<string>();
    }

    public AiStrategiesConfig GetAiStrategies(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.AiStrategies ?? new AiStrategiesConfig();
    }

    public string? GetAiPrompt(string domainId, string promptKey)
    {
        var domain = GetDomain(domainId);
        if (domain?.AiPrompts.TryGetValue(promptKey, out var prompt) == true)
        {
            return prompt;
        }
        return null;
    }

    public IntegrationConfig GetIntegrations(string domainId)
    {
        var domain = GetDomain(domainId);
        return domain?.Integrations ?? new IntegrationConfig();
    }

    public void ReloadConfiguration()
    {
        if (_config.Global.ConfigReloadEnabled)
        {
            _logger.LogInformation("Reloading domain configuration...");
            LoadConfiguration();
        }
        else
        {
            _logger.LogWarning("Configuration reload is disabled");
        }
    }

}
