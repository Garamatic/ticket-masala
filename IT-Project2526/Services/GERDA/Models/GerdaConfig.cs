using System.Text.Json.Serialization;

namespace IT_Project2526.Services.GERDA.Models;

/// <summary>
/// Configuration model for GERDA AI settings from masala_config.json
/// </summary>
public class GerdaConfig
{
    [JsonPropertyName("AppInstanceName")]
    public string AppInstanceName { get; set; } = "Ticket Masala";
    
    [JsonPropertyName("AppDescription")]
    public string AppDescription { get; set; } = string.Empty;
    
    [JsonPropertyName("DefaultSlaThresholdDays")]
    public int DefaultSlaThresholdDays { get; set; } = 30;
    
    [JsonPropertyName("GerdaAI")]
    public GerdaAISettings GerdaAI { get; set; } = new();
}

public class GerdaAISettings
{
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
    
    [JsonPropertyName("SpamDetection")]
    public SpamDetectionSettings SpamDetection { get; set; } = new();
    
    [JsonPropertyName("ComplexityEstimation")]
    public ComplexityEstimationSettings ComplexityEstimation { get; set; } = new();
    
    [JsonPropertyName("Ranking")]
    public RankingSettings Ranking { get; set; } = new();
    
    [JsonPropertyName("Dispatching")]
    public DispatchingSettings Dispatching { get; set; } = new();
    
    [JsonPropertyName("Anticipation")]
    public AnticipationSettings Anticipation { get; set; } = new();
}

/// <summary>
/// G - Grouping/Spam Detection settings
/// </summary>
public class SpamDetectionSettings
{
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
    
    [JsonPropertyName("TimeWindowMinutes")]
    public int TimeWindowMinutes { get; set; } = 60;
    
    [JsonPropertyName("MaxTicketsPerUser")]
    public int MaxTicketsPerUser { get; set; } = 5;
    
    [JsonPropertyName("Action")]
    public string Action { get; set; } = "AutoMerge"; // "AutoMerge" or "Flag"
    
    [JsonPropertyName("GroupedTicketPrefix")]
    public string GroupedTicketPrefix { get; set; } = "[GROUPED] ";
}

/// <summary>
/// E - Estimating/Complexity settings
/// </summary>
public class ComplexityEstimationSettings
{
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
    
    [JsonPropertyName("CategoryComplexityMap")]
    public List<CategoryComplexity> CategoryComplexityMap { get; set; } = new();
    
    [JsonPropertyName("DefaultEffortPoints")]
    public int DefaultEffortPoints { get; set; } = 5;
}

public class CategoryComplexity
{
    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("EffortPoints")]
    public int EffortPoints { get; set; } = 5;
}

/// <summary>
/// R - Ranking/WSJF settings
/// </summary>
public class RankingSettings
{
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
    
    [JsonPropertyName("SlaWeight")]
    public int SlaWeight { get; set; } = 100;
    
    [JsonPropertyName("ComplexityWeight")]
    public int ComplexityWeight { get; set; } = 1;
    
    [JsonPropertyName("RecalculationFrequencyMinutes")]
    public int RecalculationFrequencyMinutes { get; set; } = 1440; // Daily
}

/// <summary>
/// D - Dispatching/Recommendation settings
/// </summary>
public class DispatchingSettings
{
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
    
    [JsonPropertyName("MinHistoryForAffinityMatch")]
    public int MinHistoryForAffinityMatch { get; set; } = 3;
    
    [JsonPropertyName("MaxAssignedTicketsPerAgent")]
    public int MaxAssignedTicketsPerAgent { get; set; } = 15;
    
    [JsonPropertyName("RetrainRecommendationModelFrequencyHours")]
    public int RetrainRecommendationModelFrequencyHours { get; set; } = 24;
}

/// <summary>
/// A - Anticipation/Forecasting settings
/// </summary>
public class AnticipationSettings
{
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;
    
    [JsonPropertyName("ForecastHorizonDays")]
    public int ForecastHorizonDays { get; set; } = 30;
    
    [JsonPropertyName("InflowHistoryYears")]
    public int InflowHistoryYears { get; set; } = 3;
    
    [JsonPropertyName("CapacityRefreshFrequencyHours")]
    public int CapacityRefreshFrequencyHours { get; set; } = 12;
    
    [JsonPropertyName("RiskThresholdPercentage")]
    public int RiskThresholdPercentage { get; set; } = 20;
}
