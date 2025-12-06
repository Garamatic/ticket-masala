namespace TicketMasala.Web.Models.Configuration;

/// <summary>
/// Root configuration object loaded from masala_domains.yaml
/// </summary>
public class MasalaDomainsConfig
{
    public Dictionary<string, DomainConfig> Domains { get; set; } = new();
    public GlobalConfig Global { get; set; } = new();
}

/// <summary>
/// Configuration for a single domain (e.g., IT, Gardening, TaxLaw)
/// </summary>
public class DomainConfig
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EntityLabels EntityLabels { get; set; } = new();
    public List<WorkItemTypeDefinition> WorkItemTypes { get; set; } = new();
    public List<CustomFieldDefinition> CustomFields { get; set; } = new();
    public WorkflowConfig Workflow { get; set; } = new();
    public AiStrategiesConfig AiStrategies { get; set; } = new();
    public Dictionary<string, string> AiPrompts { get; set; } = new();
    public Dictionary<string, GerdaModelConfig> AiModels { get; set; } = new();
    public IntegrationConfig Integrations { get; set; } = new();
}

/// <summary>
/// Configuration for an AI Model (ONNX, ML.NET) and its feature map
/// </summary>
public class GerdaModelConfig
{
    public string Type { get; set; } = "ML.NET"; // ONNX, ML.NET
    public string Path { get; set; } = string.Empty;
    public List<FeatureDefinition> Features { get; set; } = new();
}

/// <summary>
/// Definition of a feature to extract from the Ticket
/// </summary>
public class FeatureDefinition
{
    public string Name { get; set; } = string.Empty;
    public string SourceField { get; set; } = string.Empty;
    public string Transformation { get; set; } = "none"; // none, min_max, one_hot
    public Dictionary<string, object> Params { get; set; } = new();
}

/// <summary>
/// Labels for universal entities in this domain
/// </summary>
public class EntityLabels
{
    public string WorkItem { get; set; } = "Ticket";
    public string WorkContainer { get; set; } = "Project";
    public string WorkHandler { get; set; } = "Agent";
}

/// <summary>
/// Definition of a work item type (e.g., Incident, Quote Request)
/// </summary>
public class WorkItemTypeDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-ticket";
    public string Color { get; set; } = "#6c757d";
    public int DefaultSlaDays { get; set; } = 7;
}

/// <summary>
/// Definition of a custom field for domain-specific data
/// </summary>
public class CustomFieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text, number, select, multi_select, date, boolean, currency
    public List<string> Options { get; set; } = new(); // For select/multi_select
    public bool Required { get; set; } = false;
    public List<string> RequiredForTypes { get; set; } = new(); // Only required for specific work item types
    public double? Min { get; set; } // For number fields
    public double? Max { get; set; } // For number fields
}

/// <summary>
/// Workflow configuration: states and transitions
/// </summary>
public class WorkflowConfig
{
    public List<WorkflowStateDefinition> States { get; set; } = new();
    public Dictionary<string, List<string>> Transitions { get; set; } = new();
    public List<TransitionRule> TransitionRules { get; set; } = new();
}

/// <summary>
/// Definition of a workflow state
/// </summary>
public class WorkflowStateDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6c757d";
}

/// <summary>
/// Advanced transition rule with conditions
/// </summary>
public class TransitionRule
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public List<TransitionCondition> Conditions { get; set; } = new();
}

/// <summary>
/// Condition for a transition rule
/// </summary>
public class TransitionCondition
{
    public string? Field { get; set; }
    public string? Operator { get; set; } // is_not_empty, >, <, ==, etc.
    public object? Value { get; set; }
    public string? Role { get; set; } // Required role to perform transition
}

/// <summary>
/// AI strategy configuration per domain
/// </summary>
public class AiStrategiesConfig
{
    public string Ranking { get; set; } = "WSJF";
    public string Dispatching { get; set; } = "MatrixFactorization";
    public string Estimating { get; set; } = "CategoryLookup";
}

/// <summary>
/// Integration configuration for ingestion and outbound
/// </summary>
public class IntegrationConfig
{
    public List<IngestionConfig> Ingestion { get; set; } = new();
    public List<OutboundConfig> Outbound { get; set; } = new();
}

/// <summary>
/// Ingestion source configuration (API, Email, CSV, ERP)
/// </summary>
public class IngestionConfig
{
    public string Type { get; set; } = string.Empty; // api, email, csv, erp_sync, webhook
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// Outbound integration configuration (Webhooks, Email, API)
/// </summary>
public class OutboundConfig
{
    public string Type { get; set; } = string.Empty; // webhook, email, api
    public string Trigger { get; set; } = string.Empty; // on_status_change, on_resolved, on_created, etc.
    public string? Url { get; set; }
    public string? Template { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// Global configuration that applies across all domains
/// </summary>
public class GlobalConfig
{
    public string DefaultDomain { get; set; } = "IT";
    public bool AllowDomainSwitching { get; set; } = false;
    public bool ConfigReloadEnabled { get; set; } = true;

}
