using IT_Project2526.Models;
using System.Text.Json;

namespace IT_Project2526.Services.GERDA.Dispatching;

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
        Customer? customer = null)
    {
        // Factor 1: ML.NET past interaction score (40% weight)
        var pastInteractionScore = mlPrediction * 0.4;

        // Factor 2: Category expertise match (30% weight)
        var expertiseScore = CalculateExpertiseScore(ticket, agent) * 0.3;

        // Factor 3: Language match (20% weight)
        var languageScore = CalculateLanguageScore(agent, customer) * 0.2;

        // Factor 4: Geographic proximity (10% weight)
        var geographyScore = CalculateGeographyScore(agent, customer) * 0.1;

        return pastInteractionScore + expertiseScore + languageScore + geographyScore;
    }

    /// <summary>
    /// Calculate expertise match score (0-5 scale)
    /// Checks if ticket category matches agent's specializations
    /// </summary>
    private static double CalculateExpertiseScore(Ticket ticket, Employee agent)
    {
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
    private static double CalculateLanguageScore(Employee agent, Customer? customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(agent.Language))
            return 3.0; // Neutral if no data

        // For now, we don't have customer language in the model
        // So we return neutral score
        // TODO: Add Language field to Customer model
        return 3.0;
    }

    /// <summary>
    /// Calculate geography match score (0-5 scale)
    /// Same region = 5.0, Different region = 2.0
    /// </summary>
    private static double CalculateGeographyScore(Employee agent, Customer? customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(agent.Region))
            return 3.0; // Neutral if no data

        // For now, we don't have customer region in the model
        // So we return neutral score
        // TODO: Add Region field to Customer model
        return 3.0;
    }

    /// <summary>
    /// Extract category from ticket description (keyword matching)
    /// Returns standardized category name for matching against specializations
    /// </summary>
    private static string ExtractCategoryFromTicket(Ticket ticket)
    {
        var description = ticket.Description?.ToLower() ?? "";

        // IT Support categories
        if (description.Contains("password") || description.Contains("login"))
            return "Password Reset";
        if (description.Contains("hardware") || description.Contains("laptop") || description.Contains("monitor"))
            return "Hardware Support";
        if (description.Contains("bug") || description.Contains("error") || description.Contains("crash"))
            return "Bug Triage";
        if (description.Contains("outage") || description.Contains("down") || description.Contains("offline"))
            return "System Outage";
        if (description.Contains("network") || description.Contains("wifi") || description.Contains("connection"))
            return "Network Troubleshooting";
        if (description.Contains("software") || description.Contains("app") || description.Contains("application"))
            return "Software Troubleshooting";

        // DevOps categories
        if (description.Contains("deployment") || description.Contains("deploy"))
            return "DevOps";
        if (description.Contains("security") || description.Contains("patch") || description.Contains("vulnerability"))
            return "Security Patch";
        if (description.Contains("performance") || description.Contains("slow"))
            return "Performance Issue";
        if (description.Contains("infrastructure") || description.Contains("server"))
            return "Infrastructure";

        // HR categories
        if (description.Contains("leave") || description.Contains("vacation") || description.Contains("pto"))
            return "Leave Request";
        if (description.Contains("payroll") || description.Contains("salary") || description.Contains("payment"))
            return "Payroll";
        if (description.Contains("onboard") || description.Contains("new hire"))
            return "Onboarding";

        // Finance/Tax categories
        if (description.Contains("refund") || description.Contains("reimburs"))
            return "Refund Request";
        if (description.Contains("tax") || description.Contains("taxes"))
            return "Tax Processing";
        if (description.Contains("fraud") || description.Contains("investigation"))
            return "Fraud Investigation";

        // Project Management
        if (description.Contains("project") || description.Contains("milestone"))
            return "Project Management";
        if (description.Contains("agile") || description.Contains("sprint"))
            return "Agile";
        if (description.Contains("risk"))
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
        Customer? customer = null)
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
