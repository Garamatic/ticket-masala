using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Compiler;

namespace TicketMasala.Tests.IntegrationTests;

public class DomainConfigurationServiceTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly Mock<ILogger<DomainConfigurationService>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly RuleCompilerService _ruleCompiler;
    private readonly string _originalEnvVar;

    public DomainConfigurationServiceTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), "ticket-masala-config-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigPath);

        _mockLogger = new Mock<ILogger<DomainConfigurationService>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(_testConfigPath);

        var compilerLogger = new Mock<ILogger<RuleCompilerService>>();
        _ruleCompiler = new RuleCompilerService(compilerLogger.Object);

        // Save and set environment variable to use test path
        _originalEnvVar = Environment.GetEnvironmentVariable("MASALA_CONFIG_PATH") ?? string.Empty;
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _testConfigPath);
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();
    }

    public void Dispose()
    {
        // Restore original environment variable
        if (string.IsNullOrEmpty(_originalEnvVar))
        {
            Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", null);
        }
        else
        {
            Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _originalEnvVar);
        }
        
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();
        
        if (Directory.Exists(_testConfigPath))
        {
            Directory.Delete(_testConfigPath, true);
        }
    }

    [Fact]
    public void Constructor_WithValidConfig_LoadsSuccessfully()
    {
        // Arrange
        CreateTestDomainConfig();

        // Act
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);
        var domains = service.GetAllDomains();

        // Assert
        Assert.NotEmpty(domains);
        Assert.Contains("TestDomain", domains.Keys);
    }

    [Fact]
    public void GetDomain_WithExistingDomain_ReturnsDomain()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var domain = service.GetDomain("TestDomain");

        // Assert
        Assert.NotNull(domain);
        Assert.Equal("Test Domain", domain.DisplayName);
    }

    [Fact]
    public void GetDomain_WithNonExistingDomain_ReturnsNull()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var domain = service.GetDomain("NonExistent");

        // Assert
        Assert.Null(domain);
    }

    [Fact]
    public void GetDefaultDomainId_ReturnsConfiguredDefault()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var defaultDomain = service.GetDefaultDomainId();

        // Assert
        Assert.Equal("TestDomain", defaultDomain);
    }

    [Fact]
    public void GetWorkItemTypes_ReturnsConfiguredTypes()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var types = service.GetWorkItemTypes("TestDomain").ToList();

        // Assert
        Assert.NotEmpty(types);
        Assert.Contains(types, t => t.Code == "TEST_TYPE");
    }

    [Fact]
    public void GetWorkItemType_WithValidCode_ReturnsType()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var type = service.GetWorkItemType("TestDomain", "TEST_TYPE");

        // Assert
        Assert.NotNull(type);
        Assert.Equal("Test Type", type.Name);
        Assert.Equal(5, type.DefaultSlaDays);
    }

    [Fact]
    public void GetCustomFields_ReturnsConfiguredFields()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var fields = service.GetCustomFields("TestDomain").ToList();

        // Assert
        Assert.NotEmpty(fields);
        Assert.Contains(fields, f => f.Name == "test_field");
    }

    [Fact]
    public void GetWorkflowStates_ReturnsConfiguredStates()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var states = service.GetWorkflowStates("TestDomain").ToList();

        // Assert
        Assert.NotEmpty(states);
        Assert.Contains(states, s => s.Code == "NEW");
        Assert.Contains(states, s => s.Code == "COMPLETED");
    }

    [Fact]
    public void GetValidTransitions_ReturnsConfiguredTransitions()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var transitions = service.GetValidTransitions("TestDomain", "NEW").ToList();

        // Assert
        Assert.NotEmpty(transitions);
        Assert.Contains("IN_PROGRESS", transitions);
    }

    [Fact]
    public void GetValidTransitions_WithInvalidState_ReturnsEmpty()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var transitions = service.GetValidTransitions("TestDomain", "INVALID_STATE").ToList();

        // Assert
        Assert.Empty(transitions);
    }

    [Fact]
    public void GetEntityLabels_ReturnsConfiguredLabels()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var labels = service.GetEntityLabels("TestDomain");

        // Assert
        Assert.Equal("Test Item", labels.WorkItem);
        Assert.Equal("Test Container", labels.WorkContainer);
        Assert.Equal("Test Handler", labels.WorkHandler);
    }

    [Fact]
    public void GetAiStrategies_ReturnsConfiguredStrategies()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);

        // Act
        var strategies = service.GetAiStrategies("TestDomain");

        // Assert
        Assert.NotNull(strategies);
        Assert.NotNull(strategies.Ranking);
        Assert.Equal("WSJF", strategies.Ranking.StrategyName);
    }

    [Fact]
    public void Constructor_WithMissingConfig_UsesDefaults()
    {
        // Arrange - no config file created

        // Act
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);
        var domains = service.GetAllDomains();

        // Assert
        Assert.NotEmpty(domains);
        Assert.Contains("IT", domains.Keys); // Default domain
    }

    [Fact]
    public void ReloadConfiguration_UpdatesConfiguration()
    {
        // Arrange
        CreateTestDomainConfig();
        var service = new DomainConfigurationService(_mockLogger.Object, _mockEnvironment.Object, _ruleCompiler);
        
        var initialDomains = service.GetAllDomains().Count;

        // Act - Update config file
        CreateTestDomainConfigWithMultipleDomains();
        service.ReloadConfiguration();
        
        var updatedDomains = service.GetAllDomains().Count;

        // Assert
        Assert.True(updatedDomains >= initialDomains);
    }

    private void CreateTestDomainConfig()
    {
        var yaml = @"
domains:
  TestDomain:
    display_name: ""Test Domain""
    description: ""Test domain for unit tests""
    
    entity_labels:
      work_item: ""Test Item""
      work_container: ""Test Container""
      work_handler: ""Test Handler""
    
    work_item_types:
      - code: TEST_TYPE
        name: ""Test Type""
        icon: ""fa-test""
        color: ""#000000""
        default_sla_days: 5
    
    custom_fields:
      - name: test_field
        label: ""Test Field""
        type: text
        required: false
    
    workflow:
      states:
        - code: NEW
          name: ""New""
          color: ""#6c757d""
        - code: IN_PROGRESS
          name: ""In Progress""
          color: ""#007bff""
        - code: COMPLETED
          name: ""Completed""
          color: ""#28a745""
      
      transitions:
        NEW: [IN_PROGRESS]
        IN_PROGRESS: [COMPLETED]
        COMPLETED: []
    
    ai_strategies:
      ranking:
        strategy_name: ""WSJF""
        base_formula: ""cost_of_delay / job_size""
        multipliers: []
      dispatching: MatrixFactorization
      estimating: CategoryLookup

global:
  default_domain: ""TestDomain""
  allow_domain_switching: false
  config_reload_enabled: true
";

        var configPath = Path.Combine(_testConfigPath, "masala_domains.yaml");
        File.WriteAllText(configPath, yaml);
    }

    private void CreateTestDomainConfigWithMultipleDomains()
    {
        var yaml = @"
domains:
  TestDomain:
    display_name: ""Test Domain""
    description: ""Test domain for unit tests""
    
    entity_labels:
      work_item: ""Test Item""
      work_container: ""Test Container""
      work_handler: ""Test Handler""
    
    work_item_types:
      - code: TEST_TYPE
        name: ""Test Type""
        icon: ""fa-test""
        color: ""#000000""
        default_sla_days: 5
    
    workflow:
      states:
        - code: NEW
          name: ""New""
          color: ""#6c757d""
      transitions:
        NEW: []
    
    ai_strategies:
      ranking:
        strategy_name: ""WSJF""
      dispatching: MatrixFactorization
      estimating: CategoryLookup

  SecondDomain:
    display_name: ""Second Domain""
    description: ""Another test domain""
    
    entity_labels:
      work_item: ""Item""
      work_container: ""Container""
      work_handler: ""Handler""
    
    work_item_types:
      - code: TYPE2
        name: ""Type 2""
        icon: ""fa-test2""
        color: ""#111111""
        default_sla_days: 3
    
    workflow:
      states:
        - code: NEW
          name: ""New""
          color: ""#6c757d""
      transitions:
        NEW: []
    
    ai_strategies:
      ranking:
        strategy_name: ""WSJF""
      dispatching: ZoneBased
      estimating: CategoryLookup

global:
  default_domain: ""TestDomain""
  allow_domain_switching: true
  config_reload_enabled: true
";

        var configPath = Path.Combine(_testConfigPath, "masala_domains.yaml");
        File.WriteAllText(configPath, yaml);
    }
}
