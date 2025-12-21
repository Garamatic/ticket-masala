using TicketMasala.Domain.Configuration;

namespace TicketMasala.Web.Engine.GERDA.Configuration;

/// <summary>
/// Service interface for accessing domain configuration.
/// This is the primary entry point for all domain-specific settings.
/// </summary>
public interface IDomainConfigurationService
{
    /// <summary>
    /// Gets the full configuration for a specific domain.
    /// </summary>
    DomainConfig? GetDomain(string domainId);

    /// <summary>
    /// Gets all configured domains.
    /// </summary>
    Dictionary<string, DomainConfig> GetAllDomains();

    /// <summary>
    /// Gets all configured domain IDs.
    /// </summary>
    IEnumerable<string> GetAllDomainIds();

    /// <summary>
    /// Gets the default domain ID from global config.
    /// </summary>
    string GetDefaultDomainId();

    /// <summary>
    /// Gets the display name for a domain.
    /// </summary>
    string GetDomainDisplayName(string domainId);

    /// <summary>
    /// Gets entity labels for a domain (e.g., "Ticket" vs "Service Visit").
    /// </summary>
    EntityLabels GetEntityLabels(string domainId);

    /// <summary>
    /// Gets all work item types configured for a domain.
    /// </summary>
    IEnumerable<WorkItemTypeDefinition> GetWorkItemTypes(string domainId);

    /// <summary>
    /// Gets a specific work item type by code.
    /// </summary>
    WorkItemTypeDefinition? GetWorkItemType(string domainId, string typeCode);

    /// <summary>
    /// Gets all custom field definitions for a domain.
    /// </summary>
    IEnumerable<CustomFieldDefinition> GetCustomFields(string domainId);

    /// <summary>
    /// Gets custom fields that are required for a specific work item type.
    /// </summary>
    IEnumerable<CustomFieldDefinition> GetRequiredFieldsForType(string domainId, string workItemTypeCode);

    /// <summary>
    /// Gets the workflow states for a domain.
    /// </summary>
    IEnumerable<WorkflowStateDefinition> GetWorkflowStates(string domainId);

    /// <summary>
    /// Gets valid next states for a given current state.
    /// </summary>
    IEnumerable<string> GetValidTransitions(string domainId, string currentState);

    /// <summary>
    /// Gets the AI strategy configuration for a domain.
    /// </summary>
    AiStrategiesConfig GetAiStrategies(string domainId);

    /// <summary>
    /// Gets an AI prompt template for a specific module (e.g., "summarize").
    /// </summary>
    string? GetAiPrompt(string domainId, string promptKey);

    /// <summary>
    /// Gets integration configuration for a domain.
    /// </summary>
    IntegrationConfig GetIntegrations(string domainId);

    /// <summary>
    /// Reloads the configuration from disk (if hot-reload is enabled).
    /// </summary>
    void ReloadConfiguration();

}
