namespace TicketMasala.Web.ViewModels.GERDA;

public class DashboardStats
{
    public int ProjectCount { get; set; }
    public int NewProjectsThisWeek { get; set; }
    public int ActiveTicketCount { get; set; }
    public int PendingTaskCount { get; set; }
    public int CompletedToday { get; set; }
    public int DueSoon { get; set; }
    public int CompletionRate { get; set; }
    public int HighRiskCount { get; set; }
    public int SentimentWarningCount { get; set; }
}
