using TicketMasala.Web.Models;
using TicketMasala.Web.Services.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Estimating;
    public class CategoryBasedEstimatingStrategy : IEstimatingStrategy
    {
        public string Name => "CategoryLookup";
        private Dictionary<string, int>? _complexityMap;

        public int EstimateComplexity(Ticket ticket, GerdaConfig config)
        {
            // Initialize map if needed
            if (_complexityMap == null)
            {
               _complexityMap = config.GerdaAI.ComplexityEstimation.CategoryComplexityMap
                .ToDictionary(
                    c => c.Category.ToLowerInvariant(),
                    c => c.EffortPoints,
                    StringComparer.OrdinalIgnoreCase);
            }

            // Try to determine category from ticket description or project
            var category = ExtractCategory(ticket, _complexityMap);
            var effortPoints = GetComplexityByCategory(category, config, _complexityMap);

            return effortPoints;
        }

        public int GetComplexityByCategory(string category, GerdaConfig config, Dictionary<string, int> complexityMap)
        {
            if (string.IsNullOrEmpty(category))
                return config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;

            // Try exact match first
            if (complexityMap.TryGetValue(category.ToLowerInvariant(), out var points))
                return points;

            // Try partial match
            var partialMatch = complexityMap.Keys
                .FirstOrDefault(k => category.ToLowerInvariant().Contains(k) || k.Contains(category.ToLowerInvariant()));

            if (partialMatch != null)
                return complexityMap[partialMatch];

            return config.GerdaAI.ComplexityEstimation.DefaultEffortPoints;
        }

        /// <summary>
        /// Extract category from ticket description or project name
        /// This is a simple keyword-based approach - could be enhanced with ML.NET text classification
        /// </summary>
        private string ExtractCategory(Ticket ticket, Dictionary<string, int> complexityMap)
        {
            var text = $"{ticket.Description} {ticket.Project?.Name}".ToLowerInvariant();

            // Check for known categories in the text
            foreach (var category in complexityMap.Keys)
            {
                if (text.Contains(category.ToLowerInvariant()))
                    return category;
            }

            // Simple keyword matching for common IT categories
            if (text.Contains("password") || text.Contains("wachtwoord") || text.Contains("login"))
                return "Password Reset";
            
            if (text.Contains("hardware") || text.Contains("laptop") || text.Contains("computer") || text.Contains("printer"))
                return "Hardware Request";
            
            if (text.Contains("bug") || text.Contains("error") || text.Contains("fout") || text.Contains("crash"))
                return "Software Bug";
            
            if (text.Contains("outage") || text.Contains("down") || text.Contains("offline") || text.Contains("storing"))
                return "System Outage";

            return "Other";
        }
}
