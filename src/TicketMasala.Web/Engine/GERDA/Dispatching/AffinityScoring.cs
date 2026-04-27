using System.Text.Json;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// Helper class for calculating 4-factor affinity scores in Dispatching service
/// </summary>
public static class AffinityScoring
{
    /// <summary>
    /// Calculate total affinity score from 4 factors
    /// Factor 1: Past Interaction (40%) - ML.NET prediction
    /// Factor 2: Category Expertise (30%) - Specialization match
    /// Factor 3: Language Match (20%) - Agent-Customer language alignment
    /// Factor 4: Geographic Proximity (10%) - Same region bonus
    /// </summary>
    public static double CalculateMultiFactorScore(
        double mlPrediction,
        Ticket ticket,
        Employee agent,
        ApplicationUser? customer = null,
        double ftsMatchScore = 0)
    {
        // Factor 1: ML.NET past interaction score (40% weight)
        var pastInteractionScore = mlPrediction * 0.4;

        // Factor 2: Category expertise match (30% weight)
        // If FTS5 score provided (V2), use Sigmoid normalization
        double expertiseScore;
        if (ftsMatchScore > 0)
        {
            // Normalize unbounded BM25 score to 0-5 range
            // Using Sigmoid centered at 5.0 with slope 1.0
            // fts=0 -> 0.03 (approx 0)
            // fts=5 -> 2.5 (mid)
            // fts=10 -> 4.9 (max)
            var normalized = 5.0 / (1.0 + Math.Exp(-(ftsMatchScore - 5.0)));
            expertiseScore = normalized * 0.3;
        }
        else
        {
            // Fallback to V1 Legacy Regex
            expertiseScore = CalculateExpertiseScore(ticket, agent) * 0.3;
        }

        // Factor 3: Language match (20% weight)
        var languageScore = CalculateLanguageScore(agent, customer) * 0.2;

        // Factor 4: Geographic proximity (10% weight)
        var geographyScore = CalculateGeographyScore(agent, customer) * 0.1;

        return pastInteractionScore + expertiseScore + languageScore + geographyScore;
    }

    public static double CalculateExpertiseScore(Ticket ticket, Employee agent)
    {
        // ... Legacy Logic kept for fallback ...
        if (string.IsNullOrWhiteSpace(agent.Specializations))
            return 2.5; // Neutral score if no specializations defined

        try
        {
            var specializations = JsonSerializer.Deserialize<List<string>>(agent.Specializations);
            if (specializations == null || !specializations.Any())
                return 2.5;

            // Extract category from ticket description (same logic as RankingService)
            var category = ExtractCategoryFromTicket(ticket);

            // Check for exact match
            if (specializations.Any(s => s.Equals(category, StringComparison.OrdinalIgnoreCase)))
                return 5.0; // Perfect match

            // Check for partial match (keywords)
            var keywords = category.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchCount = keywords.Count(keyword =>
                specializations.Any(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            if (matchCount > 0)
                return 3.5 + (matchCount * 0.5); // Partial match (3.5-4.5)

            return 2.0; // No match, but not disqualifying
        }
        catch
        {
            return 2.5; // Neutral on error
        }
    }

    /// <summary>
    /// Calculate language match score (0-5 scale)
    /// Perfect match = 5.0, Partial match = 3.5, No match = 1.0
    /// </summary>
    public static double CalculateLanguageScore(Employee agent, ApplicationUser? customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(agent.Language))
            return 3.0; // Neutral if no data

        // For now, we don't have customer language in the model
        // So we return neutral score
        if (customer == null || string.IsNullOrWhiteSpace(agent.Language) || string.IsNullOrWhiteSpace(customer.Language))
            return 3.0; // Neutral if no data

        // Check for exact match
        if (agent.Language.Equals(customer.Language, StringComparison.OrdinalIgnoreCase))
            return 5.0;

        // Check for partial match (e.g. "NL,FR" contains "NL")
        if (agent.Language.Contains(customer.Language, StringComparison.OrdinalIgnoreCase))
            return 4.5;

        return 1.0;
    }

    /// <summary>
    /// Calculate geography match score (0-5 scale)
    /// Same region = 5.0, Different region = 2.0
    /// </summary>
    public static double CalculateGeographyScore(Employee agent, ApplicationUser? customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(agent.Region))
            return 3.0; // Neutral if no data

        // For now, we don't have customer region in the model
        // So we return neutral score
        if (customer == null || string.IsNullOrWhiteSpace(agent.Region) || string.IsNullOrWhiteSpace(customer.Region))
            return 3.0; // Neutral if no data

        // Check for exact match
        if (agent.Region.Equals(customer.Region, StringComparison.OrdinalIgnoreCase))
            return 5.0;

        return 2.0;
    }

    /// <summary>
    /// Extract category from ticket description (keyword matching)
    /// Returns standardized category name for matching against specializations
    /// </summary>
    public static string ExtractCategoryFromTicket(Ticket ticket)
    {
        return ExtractCategoryFromDescription(ticket.Description);
    }

    /// <summary>
    /// Extract category from work item (keyword matching for generic IWorkItem)
    /// Returns standardized category name for matching against specializations
    /// </summary>
    public static string ExtractCategoryFromWorkItem(IWorkItem workItem)
    {
        // Try to extract from metadata if available
        try
        {
            if (!string.IsNullOrEmpty(workItem.MetadataJson))
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(workItem.MetadataJson);
                if (metadata?.TryGetValue("Title", out var title) == true && title != null)
                {
                    return ExtractCategoryFromDescription(title.ToString());
                }
            }
        }
        catch
        {
            // Fall through to use WorkType
        }

        // Default to WorkType as category
        return workItem.WorkType ?? "Other";
    }

    /// <summary>
    /// Calculate expertise score for a work item (generic version for IWorkItem)
    /// </summary>
    public static double CalculateExpertiseScore(IWorkItem workItem, Employee agent)
    {
        if (string.IsNullOrWhiteSpace(agent.Specializations))
            return 2.5;

        try
        {
            var specializations = JsonSerializer.Deserialize<List<string>>(agent.Specializations);
            if (specializations == null || !specializations.Any())
                return 2.5;

            var category = ExtractCategoryFromWorkItem(workItem);

            if (specializations.Any(s => s.Equals(category, StringComparison.OrdinalIgnoreCase)))
                return 5.0;

            var keywords = category.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchCount = keywords.Count(keyword =>
                specializations.Any(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

            if (matchCount > 0)
                return 3.5 + (matchCount * 0.5);

            return 2.0;
        }
        catch
        {
            return 2.5;
        }
    }

    /// <summary>
    /// Get explanation of affinity score breakdown for transparency (IWorkItem version)
    /// </summary>
    public static string GetScoreExplanation(
        double mlPrediction,
        IWorkItem workItem,
        Employee agent,
        ApplicationUser? customer = null)
    {
        var expertiseScore = CalculateExpertiseScore(workItem, agent);
        var languageScore = CalculateLanguageScore(agent, customer);
        var geographyScore = CalculateGeographyScore(agent, customer);
        var category = ExtractCategoryFromWorkItem(workItem);

        return $"Past Interaction: {mlPrediction:F2} (40%), " +
               $"Expertise Match ({category}): {expertiseScore:F2} (30%), " +
               $"Language: {languageScore:F2} (20%), " +
               $"Geography: {geographyScore:F2} (10%)";
    }

    private static string ExtractCategoryFromDescription(string? description)
    {
        var desc = description?.ToLower() ?? "";

        // IT Support categories
        if (desc.Contains("password") || desc.Contains("login"))
            return "Password Reset";
        if (desc.Contains("hardware") || desc.Contains("laptop") || desc.Contains("monitor"))
            return "Hardware Support";
        if (desc.Contains("bug") || desc.Contains("error") || desc.Contains("crash"))
            return "Bug Triage";
        if (desc.Contains("outage") || desc.Contains("down") || desc.Contains("offline"))
            return "System Outage";
        if (desc.Contains("network") || desc.Contains("wifi") || desc.Contains("connection"))
            return "Network Troubleshooting";
        if (desc.Contains("software") || desc.Contains("app") || desc.Contains("application"))
            return "Software Troubleshooting";

        // DevOps categories
        if (desc.Contains("deployment") || desc.Contains("deploy"))
            return "DevOps";
        if (desc.Contains("security") || desc.Contains("patch") || desc.Contains("vulnerability"))
            return "Security Patch";
        if (desc.Contains("performance") || desc.Contains("slow"))
            return "Performance Issue";
        if (desc.Contains("infrastructure") || desc.Contains("server"))
            return "Infrastructure";

        // HR categories
        if (desc.Contains("leave") || desc.Contains("vacation") || desc.Contains("pto"))
            return "Leave Request";
        if (desc.Contains("payroll") || desc.Contains("salary") || desc.Contains("payment"))
            return "Payroll";
        if (desc.Contains("onboard") || desc.Contains("new hire"))
            return "Onboarding";

        // Finance/Tax categories
        if (desc.Contains("refund") || desc.Contains("reimburs"))
            return "Refund Request";
        if (desc.Contains("tax") || desc.Contains("taxes"))
            return "Tax Processing";
        if (desc.Contains("fraud") || desc.Contains("investigation"))
            return "Fraud Investigation";

        // Project Management
        if (desc.Contains("project") || desc.Contains("milestone"))
            return "Project Management";
        if (desc.Contains("agile") || desc.Contains("sprint"))
            return "Agile";
        if (desc.Contains("risk"))
            return "Risk Management";

        return "Other"; // Default
    }

    /// <summary>
    /// Get explanation of affinity score breakdown for transparency
    /// </summary>
    public static string GetScoreExplanation(
        double mlPrediction,
        Ticket ticket,
        Employee agent,
        ApplicationUser? customer = null)
    {
        var expertiseScore = CalculateExpertiseScore(ticket, agent);
        var languageScore = CalculateLanguageScore(agent, customer);
        var geographyScore = CalculateGeographyScore(agent, customer);

        var category = ExtractCategoryFromTicket(ticket);

        return $"Past Interaction: {mlPrediction:F2} (40%), " +
               $"Expertise Match ({category}): {expertiseScore:F2} (30%), " +
               $"Language: {languageScore:F2} (20%), " +
               $"Geography: {geographyScore:F2} (10%)";
    }

}
