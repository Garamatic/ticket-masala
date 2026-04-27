namespace TicketMasala.Web.Engine.GERDA.Dispatching.Models;

/// <summary>
/// Generic agent/resource model for dispatching.
/// Used by both TicketMasala and Atom.
/// </summary>
public class Agent
{
    /// <summary>Unique agent identifier</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Agent name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Department or team</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Competencies/skills this agent has (e.g., "Hotel Tax", "Road Tax")</summary>
    public List<string> Competencies { get; set; } = new();

    /// <summary>Current number of active assignments</summary>
    public int CurrentCaseCount { get; set; }

    /// <summary>Maximum assignments allowed</summary>
    public int MaxCapacity { get; set; } = 15;

    /// <summary>Is agent currently available for assignment</summary>
    public bool IsAvailable => CurrentCaseCount < MaxCapacity;

    /// <summary>Utilization ratio (0-1)</summary>
    public decimal UtilizationRatio => MaxCapacity > 0 ? CurrentCaseCount / (decimal)MaxCapacity : 0m;

    /// <summary>Historical success rate (0-1)</summary>
    public decimal SuccessRate { get; set; } = 1.0m;

    /// <summary>Average resolution time in hours</summary>
    public decimal AverageResolutionTimeHours { get; set; }
}

/// <summary>
/// Repository interface for agent data access.
/// Implemented by both TicketMasala and Atom to work with their respective databases.
/// </summary>
public interface IAgentRepository
{
    /// <summary>Get agent by ID</summary>
    Task<Agent?> GetByIdAsync(string agentId);

    /// <summary>Get all agents who can handle a specific competency</summary>
    Task<IEnumerable<Agent>> GetAgentsByCompetencyAsync(string competency);

    /// <summary>Get all available (not overloaded) agents</summary>
    Task<IEnumerable<Agent>> GetAvailableAgentsAsync();

    /// <summary>Update agent case count</summary>
    Task UpdateCaseCountAsync(string agentId, int newCount);
}

/// <summary>
/// Repository interface for work item data access.
/// Implemented by both TicketMasala and Atom to work with their respective databases.
/// </summary>
public interface IWorkItemRepository
{
    /// <summary>Get work item by ID</summary>
    Task<IWorkItem?> GetByIdAsync(string workItemId);

    /// <summary>Get multiple work items by IDs</summary>
    Task<IEnumerable<IWorkItem>> GetByIdsAsync(IEnumerable<string> workItemIds);

    /// <summary>Get all pending/open work items</summary>
    Task<IEnumerable<IWorkItem>> GetOpenItemsAsync();
}
