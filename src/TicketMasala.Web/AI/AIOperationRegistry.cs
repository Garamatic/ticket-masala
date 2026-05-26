using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TicketMasala.Web.AI;

/// <summary>
/// Registry that maps domain operation tags to provider-specific configuration.
/// Lives in the Web/Infrastructure layer, not Domain.
/// </summary>
public sealed class AIOperationRegistry
{
    public required string DefaultModel { get; init; }
    public required string FastModel { get; init; }
    public required string QualityModel { get; init; }

    public IReadOnlyDictionary<string, AIOperationConfig> Operations { get; init; }
        = new Dictionary<string, AIOperationConfig>();
}

public sealed class AIOperationConfig
{
    /// <summary>System prompt template. {directive} and {content} placeholders.</summary>
    public required string SystemTemplate { get; init; }

    /// <summary>User prompt template. {content} placeholder.</summary>
    public string? UserTemplate { get; init; }

    /// <summary>Which model tier to use for this operation.</summary>
    public ModelTier Tier { get; init; } = ModelTier.Default;

    /// <summary>JSON schema for structured output, if any.</summary>
    public string? OutputSchema { get; init; }
}

public enum ModelTier
{
    Default,
    Fast,
    Quality,
}
