using System.Text.Json;
using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Estimating;

public class GardenComplexityStrategy : IEstimatingStrategy
{
    public string Name => "GardenComplexity";

    public int EstimateComplexity(Ticket ticket, GerdaConfig config)
    {
        // Default complexity
        int complexity = 5;

        try
        {
            if (!string.IsNullOrEmpty(ticket.DomainCustomFieldsJson))
            {
                var customFields = JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.DomainCustomFieldsJson);

                if (customFields != null && customFields.ContainsKey("garden_size_sqm"))
                {
                    // Handle potential type mismatches from JSON (string vs number)
                    var sizeObj = customFields["garden_size_sqm"];
                    if (double.TryParse(sizeObj.ToString(), out double sizeSqm))
                    {
                        if (sizeSqm < 50)
                        {
                            complexity = 3; // Small garden
                        }
                        else if (sizeSqm > 200)
                        {
                            complexity = 8; // Large garden
                        }
                        else
                        {
                            complexity = 5; // Medium garden
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback on error
            return 5;
        }

        return complexity;
    }
}
