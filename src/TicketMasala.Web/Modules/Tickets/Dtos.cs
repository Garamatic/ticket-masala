namespace TicketMasala.Web.Modules.Tickets;

public record TicketDetailsDto(
    Guid Guid,
    string Title,
    string Description,
    string Status,
    DateTime CreationDate,
    DateTime? CompletionTarget,
    string? ResponsibleName,
    string? CustomerName,
    string? ProjectName,
    double PriorityScore,
    string? GerdaTags,
    IReadOnlyList<string> ValidNextStatuses);

/// <summary>
/// Factory method to create TicketDetailsDto with proper ValidNextStatuses parsing.
/// Handles edge cases in status string formatting.
/// </summary>
public static class TicketDetailsDtoFactory
{
    public static TicketDetailsDto Create(
        Guid guid,
        string title,
        string description,
        string status,
        DateTime creationDate,
        DateTime? completionTarget,
        string? responsibleName,
        string? customerName,
        string? projectName,
        double priorityScore,
        string? gerdaTags,
        string validNextStatusesString)
    {
        // Safely parse the comma-separated statuses, handling null/empty and whitespace
        var statuses = string.IsNullOrWhiteSpace(validNextStatusesString)
            ? Array.Empty<string>()
            : validNextStatusesString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new TicketDetailsDto(
            guid, title, description, status, creationDate, completionTarget,
            responsibleName, customerName, projectName, priorityScore, gerdaTags,
            statuses);
    }
}

// Note: TicketSearchQuery is defined in Domain/Repositories/ITicketRepository.cs
// to ensure consistency across layers. It uses enum types for Status and TicketType.

public record TicketSearchResult(
    IReadOnlyList<TicketSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record TicketSummaryDto(
    Guid Guid,
    string Title,
    string Status,
    DateTime CreationDate,
    string? ResponsibleName);
