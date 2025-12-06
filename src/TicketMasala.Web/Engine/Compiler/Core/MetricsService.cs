using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Services.Core;
    /// <summary>
    /// Service responsible for calculating team metrics and dashboard data.
    /// Follows Information Expert principle - has access to ticket data needed for metrics.
    /// </summary>
    public interface IMetricsService
    {
        Task<TeamDashboardViewModel> CalculateTeamMetricsAsync();
        Task<List<ForecastData>> CalculateForecastAsync();
        Task<List<AgentPerformanceMetric>> CalculateClosedTicketsPerAgentAsync();
    }

    public class MetricsService : IMetricsService
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<MetricsService> _logger;

        public MetricsService(ITProjectDB context, ILogger<MetricsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Calculates comprehensive team metrics for the dashboard
        /// </summary>
        public async Task<TeamDashboardViewModel> CalculateTeamMetricsAsync()
        {
            _logger.LogInformation("Calculating team metrics for dashboard");

            var viewModel = new TeamDashboardViewModel();

            // Get all active tickets with related data
            var allTickets = await _context.Tickets
                .Include(t => t.Responsible)
                .Include(t => t.Customer)
                .Where(t => t.ValidUntil == null)
                .ToListAsync();

            var activeTickets = allTickets
                .Where(t => t.TicketStatus != Status.Completed)
                .ToList();

            // Calculate overall ticket metrics
            CalculateTicketMetrics(viewModel, allTickets, activeTickets);

            // Calculate GERDA AI metrics
            CalculateGerdaMetrics(viewModel, allTickets, activeTickets);

            // Calculate SLA metrics
            CalculateSlaMetrics(viewModel, activeTickets);

            // Calculate agent workload metrics
            await CalculateAgentWorkloadAsync(viewModel, activeTickets);

            // Calculate distributions
            CalculatePriorityDistribution(viewModel, activeTickets);
            CalculateComplexityDistribution(viewModel, activeTickets);

            // Calculate top tags
            CalculateTopTags(viewModel, activeTickets);

            // Calculate recent activity
            CalculateRecentActivity(viewModel, allTickets);

            _logger.LogInformation("Team metrics calculated successfully");
            return viewModel;
        }

        /// <summary>
        /// Calculate overall ticket counts and status metrics
        /// </summary>
        private void CalculateTicketMetrics(
            TeamDashboardViewModel viewModel, 
            List<Ticket> allTickets, 
            List<Ticket> activeTickets)
        {
            viewModel.TotalActiveTickets = activeTickets.Count;
            viewModel.UnassignedTickets = activeTickets.Count(t => t.ResponsibleId == null);
            viewModel.AssignedTickets = activeTickets.Count(t => t.ResponsibleId != null && t.TicketStatus != Status.Completed);
            viewModel.CompletedTickets = allTickets.Count(t => t.TicketStatus == Status.Completed);
            viewModel.OverdueTickets = activeTickets.Count(t => 
                t.CompletionTarget.HasValue && t.CompletionTarget.Value < DateTime.UtcNow);
        }

        /// <summary>
        /// Calculate GERDA AI-specific metrics
        /// </summary>
        private void CalculateGerdaMetrics(
            TeamDashboardViewModel viewModel, 
            List<Ticket> allTickets, 
            List<Ticket> activeTickets)
        {
            // Average priority score
            var ticketsWithPriority = activeTickets.Where(t => t.PriorityScore > 0).ToList();
            viewModel.AveragePriorityScore = ticketsWithPriority.Any() 
                ? Math.Round(ticketsWithPriority.Average(t => t.PriorityScore), 2) 
                : 0;

            // Average complexity
            var ticketsWithEffort = activeTickets.Where(t => t.EstimatedEffortPoints > 0).ToList();
            viewModel.AverageComplexity = ticketsWithEffort.Any() 
                ? Math.Round(ticketsWithEffort.Average(t => t.EstimatedEffortPoints), 2) 
                : 0;

            // AI assignment metrics
            viewModel.AiAssignedCount = allTickets.Count(t => 
                t.GerdaTags != null && t.GerdaTags.Contains("AI-Assigned"));

            var totalAssignments = allTickets.Count(t => t.ResponsibleId != null);
            viewModel.AiAssignmentAcceptanceRate = totalAssignments > 0 
                ? Math.Round((double)viewModel.AiAssignedCount / totalAssignments * 100, 1) 
                : 0;
        }

        /// <summary>
        /// Calculate SLA compliance metrics
        /// </summary>
        private void CalculateSlaMetrics(TeamDashboardViewModel viewModel, List<Ticket> activeTickets)
        {
            var ticketsWithSla = activeTickets.Where(t => t.CompletionTarget.HasValue).ToList();
            viewModel.TicketsWithinSla = ticketsWithSla.Count(t => t.CompletionTarget!.Value >= DateTime.UtcNow);
            viewModel.TicketsBreachingSla = ticketsWithSla.Count(t => t.CompletionTarget!.Value < DateTime.UtcNow);
            viewModel.SlaComplianceRate = ticketsWithSla.Any() 
                ? Math.Round((double)viewModel.TicketsWithinSla / ticketsWithSla.Count * 100, 1) 
                : 100;
        }

        /// <summary>
        /// Calculate agent workload and capacity utilization
        /// </summary>
        private async Task CalculateAgentWorkloadAsync(
            TeamDashboardViewModel viewModel, 
            List<Ticket> activeTickets)
        {
            var employees = await _context.Users
                .OfType<Employee>()
                .ToListAsync();

            viewModel.AgentWorkloads = employees.Select(emp =>
            {
                var assignedTickets = activeTickets
                    .Where(t => t.ResponsibleId == emp.Id && t.TicketStatus != Status.Completed)
                    .ToList();

                var workload = assignedTickets.Sum(t => t.EstimatedEffortPoints);
                var maxCapacity = emp.MaxCapacityPoints > 0 ? emp.MaxCapacityPoints : 40;

                return new AgentWorkloadMetric
                {
                    AgentId = emp.Id,
                    AgentName = $"{emp.FirstName} {emp.LastName}",
                    AssignedTicketCount = assignedTickets.Count,
                    CurrentWorkload = workload,
                    MaxCapacity = maxCapacity,
                    UtilizationPercentage = maxCapacity > 0 
                        ? Math.Round((double)workload / maxCapacity * 100, 1) 
                        : 0
                };
            }).OrderByDescending(a => a.UtilizationPercentage).ToList();
        }

        /// <summary>
        /// Calculate priority distribution for histogram
        /// </summary>
        private void CalculatePriorityDistribution(
            TeamDashboardViewModel viewModel, 
            List<Ticket> activeTickets)
        {
            foreach (var ticket in activeTickets.Where(t => t.PriorityScore > 0))
            {
                var urgencyLabel = GetUrgencyLabel(ticket.PriorityScore);
                if (viewModel.PriorityDistribution.ContainsKey(urgencyLabel))
                {
                    viewModel.PriorityDistribution[urgencyLabel]++;
                }
            }
        }

        /// <summary>
        /// Calculate complexity distribution for histogram
        /// </summary>
        private void CalculateComplexityDistribution(
            TeamDashboardViewModel viewModel, 
            List<Ticket> activeTickets)
        {
            foreach (var ticket in activeTickets.Where(t => t.EstimatedEffortPoints > 0))
            {
                var complexityLabel = GetComplexityLabel(ticket.EstimatedEffortPoints);
                if (viewModel.ComplexityDistribution.ContainsKey(complexityLabel))
                {
                    viewModel.ComplexityDistribution[complexityLabel]++;
                }
            }
        }

        /// <summary>
        /// Calculate top 10 most frequent GERDA tags
        /// </summary>
        private void CalculateTopTags(TeamDashboardViewModel viewModel, List<Ticket> activeTickets)
        {
            var allTags = activeTickets
                .Where(t => !string.IsNullOrWhiteSpace(t.GerdaTags))
                .SelectMany(t => t.GerdaTags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .GroupBy(tag => tag)
                .Select(g => new TagFrequency { TagName = g.Key, Count = g.Count() })
                .OrderByDescending(tf => tf.Count)
                .Take(10)
                .ToList();

            viewModel.TopTags = allTags;
        }

        /// <summary>
        /// Calculate recent activity timeline (last 20 events)
        /// </summary>
        private void CalculateRecentActivity(TeamDashboardViewModel viewModel, List<Ticket> allTickets)
        {
            var recentTickets = allTickets
                .OrderByDescending(t => t.CreationDate)
                .Take(20)
                .ToList();

            viewModel.RecentActivity = recentTickets.Select(t =>
            {
                string activityType;
                DateTime timestamp = t.CreationDate;

                if (t.TicketStatus == Status.Completed)
                {
                    activityType = "Completed";
                }
                else if (t.ResponsibleId != null && t.TicketStatus == Status.Assigned)
                {
                    activityType = "Assigned";
                }
                else
                {
                    activityType = "Created";
                }

                return new RecentActivityItem
                {
                    Timestamp = timestamp,
                    TicketGuid = t.Guid.ToString().Substring(0, 8),
                    TicketDescription = t.Description.Length > 60 
                        ? t.Description.Substring(0, 60) + "..." 
                        : t.Description,
                    ActivityType = activityType,
                    AgentName = t.Responsible != null 
                        ? $"{t.Responsible.FirstName} {t.Responsible.LastName}" 
                        : "Unassigned"
                };
            }).ToList();
        }

        /// <summary>
        /// Get urgency label from priority score
        /// </summary>
        private string GetUrgencyLabel(double priorityScore)
        {
            if (priorityScore >= 15.0) return "Critical";
            if (priorityScore >= 10.0) return "High";
            if (priorityScore >= 5.0) return "Medium";
            return "Low";
        }

        /// <summary>
        /// Get complexity label from effort points (Fibonacci scale)
        /// </summary>
        private string GetComplexityLabel(int effortPoints)
        {
            if (effortPoints <= 1) return "Trivial";
            if (effortPoints <= 3) return "Simple";
            if (effortPoints <= 8) return "Medium";
            if (effortPoints <= 13) return "Complex";
            return "Very Complex";
        }
        /// <summary>
        /// Calculate ticket volume forecast for the next 7 days
        /// Uses a simple moving average of the last 30 days
        /// </summary>
        public async Task<List<ForecastData>> CalculateForecastAsync()
        {
            var today = DateTime.UtcNow.Date;
            var thirtyDaysAgo = today.AddDays(-30);

            // Get daily creation counts for the last 30 days
            var dailyCounts = await _context.Tickets
                .Where(t => t.CreationDate >= thirtyDaysAgo)
                .GroupBy(t => t.CreationDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // Calculate average daily volume
            double averageDailyVolume = dailyCounts.Any() ? dailyCounts.Average(x => x.Count) : 0;
            
            // Simple linear projection with some random variation for "Peak and Valley" simulation
            // In a real app, this would use ML.NET or a more sophisticated algorithm
            var forecast = new List<ForecastData>();
            var random = new Random();

            for (int i = 1; i <= 7; i++)
            {
                var date = today.AddDays(i);
                // Simulate weekday vs weekend variance
                var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                var variance = (random.NextDouble() * 0.4) - 0.2; // +/- 20% variance
                var volume = averageDailyVolume * (1 + variance);
                
                if (isWeekend) volume *= 0.3; // Lower volume on weekends

                forecast.Add(new ForecastData
                {
                    Date = date,
                    PredictedVolume = (int)Math.Round(Math.Max(0, volume))
                });
            }

            return forecast;
        }

        /// <summary>
        /// Calculate closed tickets per agent for performance tracking
        /// </summary>
        public async Task<List<AgentPerformanceMetric>> CalculateClosedTicketsPerAgentAsync()
        {
            var employees = await _context.Users
                .OfType<Employee>()
                .ToListAsync();

            var metrics = new List<AgentPerformanceMetric>();

            foreach (var emp in employees)
            {
                var closedCount = await _context.Tickets
                    .CountAsync(t => t.ResponsibleId == emp.Id && t.TicketStatus == Status.Completed);

                metrics.Add(new AgentPerformanceMetric
                {
                    AgentId = emp.Id,
                    AgentName = $"{emp.FirstName} {emp.LastName}",
                    ClosedTickets = closedCount
                });
            }

            return metrics.OrderByDescending(m => m.ClosedTickets).ToList();
        }
}
