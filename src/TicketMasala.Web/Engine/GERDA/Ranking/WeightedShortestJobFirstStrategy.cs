using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Ranking;
    public class WeightedShortestJobFirstStrategy : IJobRankingStrategy
    {
        public string Name => "WSJF";

        public double CalculateScore(Ticket ticket, GerdaConfig config)
        {
            // Calculate Cost of Delay (urgency based on SLA and age)
            var costOfDelay = CalculateCostOfDelay(ticket, config);

            // Get Job Size (effort points from Estimating service)
            var jobSize = ticket.EstimatedEffortPoints > 0 ? ticket.EstimatedEffortPoints : 5; // Default to medium

            // WSJF Formula: Priority = Cost of Delay / Job Size
            return costOfDelay / (double)jobSize;
        }

        /// <summary>
        /// Calculate Cost of Delay based on SLA breach risk, ticket age, and category-specific urgency
        /// </summary>
        private double CalculateCostOfDelay(Ticket ticket, GerdaConfig config)
        {
            var now = DateTime.UtcNow;
            var age = (now - ticket.CreationDate).TotalDays;

            // Get category-specific urgency multiplier from queue config
            var categoryMultiplier = GetCategoryUrgencyMultiplier(ticket, config);

            // If ticket has a completion target (SLA), calculate urgency based on that
            if (ticket.CompletionTarget.HasValue)
            {
                var daysUntilDeadline = (ticket.CompletionTarget.Value - now).TotalDays;

                if (daysUntilDeadline <= 0)
                {
                    // Already breached SLA - CRITICAL
                    return config.GerdaAI.Ranking.SlaWeight * 10.0 * categoryMultiplier;
                }
                else if (daysUntilDeadline <= 1)
                {
                    // Less than 1 day until breach - URGENT
                    return config.GerdaAI.Ranking.SlaWeight * 5.0 * categoryMultiplier;
                }
                else if (daysUntilDeadline <= 3)
                {
                    // Less than 3 days until breach - HIGH
                    return config.GerdaAI.Ranking.SlaWeight * 2.0 * categoryMultiplier;
                }
                else
                {
                    // Normal urgency based on time remaining
                    // More time remaining = lower urgency
                    return (config.GerdaAI.Ranking.SlaWeight / daysUntilDeadline) * categoryMultiplier;
                }
            }

            // Fallback: use ticket age as urgency factor
            // Older tickets get higher urgency
            return (age * config.GerdaAI.Ranking.SlaWeight / 10.0) * categoryMultiplier;
        }

        /// <summary>
        /// Get category-specific urgency multiplier from queue configuration
        /// </summary>
        private double GetCategoryUrgencyMultiplier(Ticket ticket, GerdaConfig config)
        {
            // Find the queue config for this ticket's domain
            var queueConfig = config.Queues.FirstOrDefault(q => 
                q.Code == ticket.DomainId);

            if (queueConfig == null)
            {
                return 1.0; // Default multiplier if no queue config found
            }

            // Get the ticket's category/description as a potential category match
            var category = ExtractCategoryFromTicket(ticket);

            if (queueConfig.UrgencyMultipliers.TryGetValue(category, out var multiplier))
            {
                return multiplier;
            }

            // Try "Other" as fallback
            if (queueConfig.UrgencyMultipliers.TryGetValue("Other", out var otherMultiplier))
            {
                return otherMultiplier;
            }

            return 1.0; // Default if no match found
        }

        /// <summary>
        /// Extract category from ticket description or title
        /// Maps ticket content to configured categories
        /// </summary>
        private string ExtractCategoryFromTicket(Ticket ticket)
        {
            var description = ticket.Description?.ToLower() ?? "";
            
            // Simple keyword matching - in production this could use ML classification
            if (description.Contains("password") || description.Contains("login"))
                return "Password Reset";
            if (description.Contains("hardware") || description.Contains("laptop") || description.Contains("monitor"))
                return "Hardware Request";
            if (description.Contains("bug") || description.Contains("error") || description.Contains("crash"))
                return "Software Bug";
            if (description.Contains("outage") || description.Contains("down") || description.Contains("offline"))
                return "System Outage";
            if (description.Contains("deployment") || description.Contains("deploy"))
                return "Deployment";
            if (description.Contains("security") || description.Contains("patch") || description.Contains("vulnerability"))
                return "Security Patch";
            if (description.Contains("performance") || description.Contains("slow"))
                return "Performance Issue";
            if (description.Contains("leave") || description.Contains("vacation") || description.Contains("pto"))
                return "Leave Request";
            if (description.Contains("payroll") || description.Contains("salary") || description.Contains("payment"))
                return "Payroll Issue";
            if (description.Contains("onboard") || description.Contains("new hire"))
                return "Onboarding";
            if (description.Contains("refund") || description.Contains("reimburs"))
                return "Refund Request";
            if (description.Contains("fraud") || description.Contains("investigation"))
                return "Fraud Investigation";

            return "Other"; // Default category
        }
        
        /// <summary>
        /// Get queue code from ProjectGuid (stub - implement based on your project data model)
        /// </summary>
        private string GetQueueCodeFromProjectGuid(Guid projectGuid)
        {
            // This is a simplified mapping - in production you'd query the Project table
            // or maintain a ProjectGuid -> QueueCode lookup
            // For now, return a default value
            return "ITCS"; // Default queue code
        }
}
