namespace TicketMasala.Web.ViewModels.Projects;

using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// ViewModel for creating a project from an existing ticket
/// </summary>
public class CreateProjectFromTicketViewModel
{
    /// <summary>
    /// The source ticket ID
    /// </summary>
    public Guid TicketId { get; set; }
    
    /// <summary>
    /// Ticket description/subject for context
    /// </summary>
    public string TicketDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer name from the ticket
    /// </summary>
    public string? CustomerName { get; set; }
    
    /// <summary>
    /// Customer ID from the ticket
    /// </summary>
    public string? CustomerId { get; set; }
    
    /// <summary>
    /// Project name (pre-filled from ticket subject)
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
    
    /// <summary>
    /// Project description
    /// </summary>
    public string? ProjectDescription { get; set; }
    
    /// <summary>
    /// Selected template ID
    /// </summary>
    public Guid? SelectedTemplateId { get; set; }
    
    /// <summary>
    /// Available templates
    /// </summary>
    public SelectList? TemplateList { get; set; }
    
    /// <summary>
    /// GERDA-recommended Project Manager ID
    /// </summary>
    public string? RecommendedPMId { get; set; }
    
    /// <summary>
    /// Recommended PM's name for display
    /// </summary>
    public string? RecommendedPMName { get; set; }
    
    /// <summary>
    /// Selected PM (can override GERDA recommendation)
    /// </summary>
    public string? SelectedPMId { get; set; }
    
    /// <summary>
    /// Available project managers
    /// </summary>
    public SelectList? ProjectManagerList { get; set; }
    
    /// <summary>
    /// Target completion date
    /// </summary>
    public DateTime? TargetCompletionDate { get; set; }

}
